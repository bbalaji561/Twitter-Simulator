#time "on"
#r "nuget: Akka"
#r "nuget: Akka.FSharp"
#r "nuget: Akka.Remote"
#r "nuget: Akka.TestKit"
open System
open Akka.Actor
open Akka.FSharp
open Akka.Configuration

// Input from Command Line
let myIP = fsi.CommandLineArgs.[1] |> string
let port = fsi.CommandLineArgs.[2] |> string
let id = fsi.CommandLineArgs.[3] |> string
let users = fsi.CommandLineArgs.[4] |> string
let noofclients = fsi.CommandLineArgs.[5] |> string
let serverip = fsi.CommandLineArgs.[6] |> string

// Configuration
let configuration = 
    ConfigurationFactory.ParseString(
        sprintf @"akka {            
            stdout-loglevel : DEBUG
            loglevel : ERROR
            actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            }
            remote.helios.tcp {
                transport-protocol = tcp
                port = %s
                hostname = %s
            }
    }" port myIP)

let system = ActorSystem.Create("TwitterClient", configuration)
type BossMessages = 
    | Start of (int*int*int*string)
    | RegisterUser of (int)
    | Offline of (string)
    | Received of (int)
    | AckUserReg of (string*string)

type FollowMessages = 
    | Init of (list<string>*int)

type UserMessages = 
    | Ready of (string*list<string>*ActorSelection*int*string*List<string>*int)
    | GoOnline
    | GoOffline
    | Action
    | ActionTweet

// Printer Actor - To print the output
let printerActor (mailbox:Actor<_>) = 
    let rec loop () = actor {
        let! (message:obj) = mailbox.Receive()
        printfn "%A" message
        return! loop()
    }
    loop()
let printerRef = spawn system "Printer" printerActor

//1 - Tweet, 2 - Retweet, 3 - Follow, 4 - Tweet with only hashtags, 5 - Tweet with mentions and hashtags, 6 - QueryHashtags, 7 - QueryMentions
let UserActors (mailbox:Actor<_>) = 
    let mutable myId = ""
    let mutable isOnline = false
    let mutable clientList = []
    let mutable server = ActorSelection()
    let mutable usersCount = 0
    let followRand = Random()
    let htagRand = Random()
    let mentionsRand = Random()
    let htagRandReq = Random()
    let mentionsRandReq = Random()
    let clientRand = Random()
    let mutable cliId = ""
    let mutable topHashTags = []
    let mutable tweetCount = 0
    let mutable interval = 0.0
    let rec loop () = actor {
        let! message = mailbox.Receive() 
        match message with
        | Ready(mid,clist,ser,nusers,cid,htList,time) ->
            myId <- mid
            clientList <- clist
            isOnline <- true
            server <- ser
            usersCount <- nusers
            cliId <- cid
            topHashTags <- htList
            interval <- time |> float
            system.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(50.0), mailbox.Self, Action)
            system.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(49.0), mailbox.Self, ActionTweet)
        | Action ->
            if isOnline then
                let actions = [3; 6; 7]
                let actionsrand = Random()
                let act = actions.[actionsrand.Next(actions.Length)]
                match act with
                | 3 ->
                    let mutable fUser = [1 .. usersCount].[followRand.Next(usersCount)] |> string
                    let mutable randclid = clientList.[clientRand.Next(clientList.Length)]
                    let mutable followUser = sprintf "%s_%s" randclid fUser
                    while followUser = myId do 
                        fUser <- [1 .. usersCount].[followRand.Next(usersCount)] |> string
                        followUser <- sprintf "%s_%s" randclid fUser 
                    server <! ("Follow",cliId,myId,followUser,DateTime.Now)
                | 7 ->
                    let hashTag = topHashTags.[htagRandReq.Next(topHashTags.Length)]
                    server <! ("QueryHashtags",cliId,myId,hashTag,DateTime.Now)
                | 6 ->
                    let mutable mUser = [1 .. usersCount].[mentionsRandReq.Next(usersCount)] |> string
                    let mutable randclid = clientList.[clientRand.Next(clientList.Length)]
                    let mutable mentionsUser = sprintf "%s_%s" randclid mUser
                    server <! ("QueryMentions",cliId,myId,mentionsUser,DateTime.Now)
                | _ ->
                    ignore()
                system.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(100.0), mailbox.Self, Action)
        | ActionTweet ->
            if isOnline then
                let actions = [4; 1; 2; 5]
                let actionsrand = Random()
                let act = actions.[actionsrand.Next(actions.Length)]
                match act with   
                | 1 ->
                    let timestamp = DateTime.Now
                    tweetCount <- tweetCount+1
                    let tweetMsg = sprintf "%s tweeted -> tweet_%d" myId tweetCount
                    server <! ("Tweet",cliId,myId,tweetMsg,timestamp)
                | 2 ->
                    let timestamp = DateTime.Now
                    server <! ("ReTweet",cliId,myId,sprintf "user %s doing re-tweet" myId,timestamp)  
                | 4 ->
                    let timestamp = DateTime.Now
                    let hashTag = topHashTags.[htagRand.Next(topHashTags.Length)]
                    tweetCount <- tweetCount+1
                    let tweetMsg = sprintf "%s tweeted -> tweet_%d with hashtag #%s" myId tweetCount hashTag
                    server <! ("Tweet",cliId,myId,tweetMsg,timestamp)
                | 5 ->
                    let timestamp = DateTime.Now
                    let mutable mUser = [1 .. usersCount].[mentionsRand.Next(usersCount)] |> string
                    let mutable randclid = clientList.[clientRand.Next(clientList.Length)]
                    let mutable mentionsUser = sprintf "%s_%s" randclid mUser
                    while mentionsUser = myId do 
                        mUser <- [1 .. usersCount].[mentionsRand.Next(usersCount)] |> string
                        mentionsUser <- sprintf "%s_%s" randclid mUser 
                    let hashTag = topHashTags.[htagRand.Next(topHashTags.Length)]
                    tweetCount <- tweetCount+1
                    let tweetMsg = sprintf "%s tweeted tweet_%d with hashtag #%s and mentioned @%s" myId tweetCount hashTag mentionsUser
                    server <! ("Tweet",cliId,myId,tweetMsg,timestamp) 
                | _ ->
                    ignore()   
                system.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(interval), mailbox.Self, ActionTweet)                                                           
        | GoOffline ->
            isOnline <- false
        | GoOnline ->
            isOnline <- true
            system.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(100.0), mailbox.Self, Action)
            system.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(101.0), mailbox.Self, ActionTweet)
        return! loop()
    }
    loop()

let ClientAdminActor (mailbox:Actor<_>) = 
    let mutable id = ""
    let mutable nusers = 0
    let mutable nclients = 0
    let mutable port = ""
    let mutable clientslist = []
    let mutable cur_offline = Set.empty
    let mutable registered_list = []
    let mutable useraddress = Map.empty
    let mutable intervalmap = Map.empty
    let mutable usersList = []
    let mutable subsrank = Map.empty
    let server = system.ActorSelection(
                   sprintf "akka.tcp://TwitterServer@%s:8776/user/ServerActor" serverip)
    let hashTagsList = ["lockdown";"metoo";"covid19";"blacklivesmatter";"crypto";"crowdfunding";"giveaway";"contest";
                        "blackhistorymonth";"womenshistorymonth";"cryptocurrency";"womensday";"happybirthday";
                        "authentication";"USelections";"bidenharris";"internationalwomensday";"influencermarketing";
                        "distributedsystems";"gogators";"blackfriday";"funny";"womeninstem";"iwon";"photography";
                        "mondaymotivation";"ootd";"vegan";"traveltuesday";"tbt"]

    let rec loop () = actor {
        let! (message:obj) = mailbox.Receive() 
        let (mtype,_,_,_,_) : Tuple<string,string,string,string,string> = downcast message
        match mtype with
        | "Start" ->
            let timestamp = DateTime.Now
            let (_,i, u, n, p) : Tuple<string,string,string,string,string> = downcast message 
            printerRef <! sprintf "Client %s Start!!" id
            id <- i
            nusers <- u |> int32
            nclients <- n |> int32
            port <- p
            let mutable usersarr = [| 1 .. nusers |]
            // printfn "usersarr=%A" usersarr
            let rand = Random()
            let swap (a: _[]) x y =
                let tmp = a.[x]
                a.[x] <- a.[y]
                a.[y] <- tmp
            let shuffle a =
                Array.iteri (fun i _ -> swap a i (rand.Next(i, Array.length a))) a
            
            shuffle usersarr
            usersList <- usersarr |> Array.toList
            // printfn "second userarr=%A" usersarr
            for i in [1 .. nusers] do
                let userkey = usersarr.[i-1] |> string
                subsrank <- Map.add (sprintf "%s_%s" id userkey) ((nusers-1)/i) subsrank
                intervalmap <- Map.add (sprintf "%s_%s" id userkey) i intervalmap

            server <! ("ClientRegister",id,myIP,port,timestamp)
            for i in [1 .. nclients] do
                let istr = i |> string
                clientslist <- istr :: clientslist
        | "AckClientReg" ->
            mailbox.Self <! ("RegisterUser","1","","","")
            system.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(5.0), mailbox.Self, ("Offline","","","",""))
        | "RegisterUser" ->
            let timestamp = DateTime.Now
            let (_,nextid,_,_,_) : Tuple<string,string,string,string,string> = downcast message 
            let mutable numcurid = nextid |> int32
            let mutable curid = sprintf "%s_%s" id (usersList.[numcurid-1] |> string) 
            let ref = spawn system (sprintf "User_%s" curid) UserActors
            useraddress <- Map.add curid ref useraddress
            let subsstr = subsrank.[curid] |> string
            server <! ("UserRegister", id, curid, subsstr,timestamp)
            registered_list <- curid :: registered_list

            if numcurid < nusers then
                numcurid <- numcurid+1
                let stnumcurid = numcurid |> string
                system.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(50.0), mailbox.Self, ("RegisterUser",stnumcurid,"","",""))
        | "AckUserReg" ->
            let (_,uid,msg,_,_) : Tuple<string,string,string,string,string> = downcast message 
            printerRef <! msg
            let mutable baseInterval = nusers/100
            if baseInterval < 5 then
                baseInterval <- 5            
            useraddress.[uid] <! Ready(uid,clientslist,server,nusers,id,hashTagsList,(baseInterval*intervalmap.[uid]))
        | "Offline" ->
            let timestamp = DateTime.Now
            // printerRef <! sprintf "Users going offline & online: %A" cur_offline
            let offlinerand = Random()
            let mutable total = registered_list.Length
            total <- (30*total)/100
            let mutable newset = Set.empty
            for i in [1 .. total] do
                let mutable nextoffline = registered_list.[offlinerand.Next(registered_list.Length)]
                while cur_offline.Contains(nextoffline) || newset.Contains(nextoffline) do
                    nextoffline <- registered_list.[offlinerand.Next(registered_list.Length)]
                server <! ("GoOffline", id, nextoffline, "", timestamp)
                useraddress.[nextoffline] <! GoOffline
                newset <- Set.add nextoffline newset

            for goonline in cur_offline do
                server <! ("GoOnline", id, goonline, "",timestamp)
                
            // printerRef <! sprintf "new_set: %A" newset
            cur_offline <- Set.empty
            cur_offline <- newset
            // printerRef <! sprintf "new offline: %A" cur_offline
            system.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(5.0), mailbox.Self, ("Offline","","","",""))
        | "AckOnline" ->
            let (_,uid,_,_,_) : Tuple<string,string,string,string,string> = downcast message 
            useraddress.[uid] <! GoOnline
        | (_) ->
            ignore()
        return! loop()
    }
    loop()

// Start of the algorithm - spawn Boss, the delgator
let boss = spawn system "AdminActor" ClientAdminActor
boss <! ("Start", id, users, noofclients, port)
system.WhenTerminated.Wait()