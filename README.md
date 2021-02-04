# Twitter-Simulator
Implement a Twitter like engine using the Akka actors model and support the following functionalities: Register, Tweet, Subscribe, Re-Tweet and Query specific to hashtags and mentions. Also have to develop a client simulator for generating the requests and subscribers are distributed based on Zipf distribution model.

### Considerations: 
A single client process simulates the given input number of users(command line argument). To simulate from multiple clients, start a new client process in the new terminals with modified command line arguments.

### Commands to Run
First have to start the server and then client/clients as required. Server always runs on port 8776 on the machine when started. <server-ip>, <client-ip>, and <client-port> are the IP address and port number of the server and client processes and have to be passed to the script.

#### Server #### 
dotnet fsi --langversion:preview twitterserver.fsx <server-ip>

#### Client ####
dotnet fsi --langversion:preview twitterclient.fsx <client-ip> <client-port> <client-id> <number-of-users> <total-number-of-clients> <server-ip>

Inorder to make the program function correctly, the following considerations are required.
* \<client-id> should always start from 1, 2, ... and so on and should be sequential if multiple clients are started.
* \<total-number-of-clients> indicates the number of clients you would like to start in multiple terminals and should be the same value for all the clients. The mentioned number of client processes must be started.
* \<number-of-users> indicates the number of users to be simulated in a single client. If multiple clients are started, this value has to be the same across all the clients to make the Zipf distribution.
    
​For example,​ to simulate 3000 users using three clients, we need to start the clients as below:
* Terminal 1 - dotnet fsi --langversion:preview twitterclient.fsx <client-ip> <client-port> 1 1000 3 <server-ip>
* Terminal 2 - dotnet fsi --langversion:preview twitterclient.fsx <client-ip> <client-port> 2 1000 3 <server-ip>
* Terminal 3 - dotnet fsi --langversion:preview twitterclient.fsx <client-ip> <client-port> 3 1000 3 <server-ip>

### Implementation
Implemented the project by dividing it into 2 parts, TwitterServer and TwitterClient.
#### TwitterServer
TwitterServer is our server engine and it supports the following services: ​Client Registration, User Registration, Online/Offline of users, Tweet, re-tweet, Subscribe/Follow, Query HashTags or MentionUsers by using akka actors for each service to handle the client requests.
Building in-memory tables by using the map data structure in F# to store the data. The data required for each service are stored in their respective actors and are not shared to make them work independently.
#### TwitterClient
TwitterClient is our client engine. A single client process can simulate a given number of users. Each client is an individual process. Multiple client processes can be started to simulate more users. Each client first spawns ​a UserActor for each user and then registers the users with the server. After getting the acknowledgement, the user actor randomly performs actions like Tweet, Re-tweet, follow, Query hashtags or mentions at a regular interval based on its zipf distribution rank which is explained in the next section. A list of 30 popular hashtags is pre-populated and stored in the client to select hashtags. To simulate the live connections, for every 5 seconds it randomly makes 30% of users to go offline. Detailed description or functionality for each of the actors at the server and client side are provided at the end of the report.
### ARCHITECTURE
![architecture](https://github.com/bbalaji561/Twitter-Simulator/blob/main/Images/Architecture.png)

### Zipf distribution
1. To follow the zipf distribution, we assign a rank for each of the users randomly. The users with the first rank will have the highest number of followers which is equal to (total users per client)-1. Users with second rank will have (total users per client-1)/2 followers and third rank will have (total users per client-1)/3 and so on.
2. This forms a zipf distribution on the number of subscribers. Now, to accordingly distribute the tweets, we calculate the frequency at which user tweets based on the user’s rank. The first ranked user tweets more often than the second, and the second tweets more often than the third and so on.
3. For example, if there are 100 users, the first ranked user will have 99 followers, second user will have 49, third will have 33 and so on. The frequency of tweets for the first ranked user, say, 1ms, then second would have 2ms. third would have 3ms and so on. Thus, say for 100 ms, first ranked user tweets 100 times, second tweets 50, third tweets 33 and finally the 100th ranked user tweets just once.
4. The above logistics is per client basis. If multiple clients are started, then it follows the same logic for each client independently and still satisfies the zipf distribution by forming batches for each rank.
5. For example, 3 clients with 100 users each, there will be 3 users for each rank with the same number of followers and tweets frequency. In overall, all the users will follow zipf distribution with respect to their ranks. 6. Thus the number of users with maximum subscribers is less than the number of users with minimum to 1 subscribers and so zipf distribution is achieved.

#### Program Termination:
We could have made the program terminate after reaching a threshold like the number of requests handled or if all the users had reached a tweet count. But, since it is a simulation with few users going online/offline at a 5 second interval, we decided to keep it running simulating the live connections. To terminate the program, hit ctrl+c or cmd+c in the terminal.

#### Logging:
Every 5 seconds, the server terminal prints the average number of requests it handled per second from the start time. The client terminals print the log for each service requested by the users at that client. It includes, timestamp, service requested and the other details as shown below. For easy understanding of the logs, we designed the tweet messages itself to look like a log. In the below example “​tweet_6387 with hashtag #USelections and mentioned @2_7"​ is itself the tweet message.

- "[12/2/2020 4:43:15 PM][TWEET] 1_3 tweeted -> tweet_6387 with hashtag #USelections and mentioned @2_7"
- "[12/2/2020 4:43:15 PM][NEW_FEED] For User: 1_1 -> 1_3 tweeted tweet_6387 with hashtag #USelections and mentioned @2_7" 
- "[12/2/2020 4:43:15 PM][RE_TWEET] 1_9 retweeted -> 1_4 tweeted tweet_1140 with hashtag #iwon and mentioned @1_10" 
- "[12/2/2020 4:44:02 PM][FOLLOW] User 2_10 started following 1_9"

#### PerfStats.txt
This file contains stats for each user participating with the number of subscribers and total number of tweets/retweets performed by the user. It also contains the average number of requests handled by the server per second from the start to the end.

#### Limitations:
- ​For every 5 seconds, the server will update the stats file in the same directory. If the file looks corrupted, it might be because the program is terminated while writing to the file. Please re-run.
- We are using unsigned int64 for counting the number of requests handled at the server. So the limit is bounded by the maximum value of uint64 which is 18446744073709551615.

#### Development Environment
Mac-book Air, Dual Core with 4 Threads, 8GB RAM

#### Largest Network 
We simulated 50000 users across 2 client processes. We observed an average of 4500 requests served per second by the server when there were around 10000 users. After removing all the print and log statements, the average time taken by each service for a request is noticed to vary between 20ms to 1500 ms approximately depending on the number of users and requests handled.

### Performance Evaluation
#### Zipf distribution on number of subscribers:
Data considered for graph:​ 1 client having 1000 users and server handled 10L requests
![graph_1](https://github.com/bbalaji561/Twitter-Simulator/blob/main/Images/Graph_1.png)   

![graph_2](https://github.com/bbalaji561/Twitter-Simulator/blob/main/Images/Graph_2.png)

![graph_3](https://github.com/bbalaji561/Twitter-Simulator/blob/main/Images/Graph_3.png)
   
#### Server Performance:
Note: To calculate the actual time taken for each service at the server, we commented out all the print statements and logging at the client side, because continuous print and log statements in the terminal occupy the thread and do not produce the actual server performance. We tested this in a dual core 4 threaded machine.

We have analysed the average number of requests that the server handled per second for various input values and provided below a sample of it. We observed that when the number of users increases, the simulation increases and the number of requests handled also increases till a threshold value. After that, the requests handled per second stays more or less the same. We have seen a maximum of 4500(approx) requests served per second when simulated 10000 users.

![Table_1](https://github.com/bbalaji561/Twitter-Simulator/blob/main/Images/Table_1.png)

![Table_2](https://github.com/bbalaji561/Twitter-Simulator/blob/main/Images/Table_2.png)

#### Observations:
We observed that when the number of users are between 1000 and 5000, the average requests handled by the server reaches maximum potential. If the users are too low or very high, average requests handled decreases comparatively. Also, the average time taken for each service increases when the number of users and client processes increases because of many requests generated.

#### Description/Functionality of Server side actors:
#### Server Actor:
Server actor is the boss actor which acts as the interface to receive the requests from the clients and delegates the requests to the corresponding actor for that service.

#### UsersActor:
Handles users registration, online/offline of users, and follow other users.
- User Registration : When there is a request for user registration we store the details of registered users in a set and also add an entry (as a key) for the same in the followers map
- Offline/Online : At a periodic interval, a certain percentage of active users become inactive and few who are inactive before become active.
Whenever an user becomes active it retrieves its latest 10 feeds from the showfeedactor and also when it becomes offline, it will store it’s feeds in the same. Details of this functionality will be explained in showfeedactor in the document later.
- Follow : When a user subscribes another user, the requesting user will be added to the requested user’s following list. It will be added only when it satisfies the following conditions: The requested user must be registered, it should not already contain the requesting user and the requested user’s followers list length is less than its subscribers limit count(per Zipf distribution).
- UpdateFeeds : This handler is used by the tweet and retweet actors. Whenever a user tweets or retweets, the engine has to post/update the new feed to its followers/subscribers. For its active followers it stores the new tweet in it’s showfeedactor’s table and also the new tweet appears on the terminal but for inactive users it just stores the new tweet in it’s showfeedactor’s table and will be shown when it becomes online later in future.

#### MentionsActor:
Checks for any mentions in the tweet using the special character ‘@’ to parse for a mentioned user in the tweet.   
- ParseMentions : Whenever a user posts a new tweet, it is parsed to check if it has any mentions and it is already registered. If it is, only then the tweet is passed to the tweetactor to perform further actions and the respective tweet is added to the mentioned user’s list of tweets in the map.
- QueryMentions : When a user queries for a specific mentioned user, it checks if it is present in the mentionsMap and replies to the query with a max of latest 10 tweets from its list.

#### TweetActor:
Whenever a user posts a new tweet and the request arrives to the TweetActor, it means that the tweet is valid. Further actions performed on it are as follows, first it increments the tweet count for the corresponding user. Then it sends the tweet to hashtagactor. At last, it requests the usersactor to update the tweet to it’s active followers list.

#### HashtagActor:
Checks for any hashtags in the tweet using the special character ‘#’ to parse for a hashtag in the tweet. 
- ParseHashTag : Whenever a user posts a new tweet, it is parsed to check if it has any hashtags present. If it is, only then the tweet is stored in the mentioned user’s list of tweets in the map.
- QueryHashTags : When a user queries for a specific hashtag, it checks if it is present in the hashtagsMap and replies to the query with a max of latest 10 tweets from its list.

#### Retweetactor:
When the user posts a new tweet, these valid tweets are stored in retweetfeedtable for the user’s followers. Later when the retweet request arrives a random tweet is selected and calls the users actor to update the retweet to its followers/subscribers.

#### Showfeedactor:
When users post a new tweet, this actor is used to store and update the feed table for the user’s followers. 
- Showfeeds : When the user becomes online after a certain interval, the active user retrieves it’s latest max of 10 tweets from feedtable and is shown on it’s feed. If no feed is present then it says it is empty.
-UpdateFeedTable : Whenever a user posts a new tweet / retweets, the tweet is added to it’s subscribers feed table.

#### Description/Functionality of Client side actors:
#### UsersActor:
Client is simulated to generate random requests for the following services at periodic intervals: tweet, retweet, follow, query for hashtag and mentioned user and offline/online.

#### Tweet/Retweet
The user performs randomly one of the 3 kinds of tweets - First without hashtag and mentions, second with one hashtag, and last with a hashtag and a mention. If a hashtag is to be present, it selects one hashtag randomly from the prepopulated hashtags list. If a mentioned user needs to be present, it constructs a user id with the combination of a random client number selected from the input and random user from the list of users present.
- Follow : A follow user id is constructed randomly by selecting a random client number from the input and random user from the list of users present. A follow request to the follower id from the user id is generated and sent to the server.
- Query Hashtags/Mentions : The user queries for a list of tweets from the server for a randomly picked hashtag or mentioned user.
- Online/Offline : 30% of the users at a periodic interval of 5 seconds go ​offline.​ The inactive users from the previous cycle become active in the current online cycle.
