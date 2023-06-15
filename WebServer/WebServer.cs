/// <summary>
/// Author:    Draeden Jensen and Derek Kober
/// Date:      04-24-2023
/// Course:    CS 3500, University of Utah, School of Computing
/// Copyright: CS 3500, Draeden Jensen - This work may not 
///            be copied for use in Academic Coursework.
///
/// We, Draeden Jensen and Derek Kober, certify that this code was 
/// written from scratch and we did not copy it in part or whole from 
/// another source.  All references used in the completion of the assignments
/// are cited in the README file.
///
/// File Contents:
/// Contains a web server for viewing data from the Agario database.
/// </summary>

using Communications;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace WebServer
{
    /// <summary>
    /// Author:   H. James de St. Germain
    /// Date:     Spring 2020
    /// Updated:  Spring 2023
    ///
    /// Code for a simple web server
    /// </summary>
    class WebServer
    {
        /// <summary>
        /// keep track of how many requests have come in.  Just used
        /// for display purposes.
        /// </summary>
        static private int counter = 1;

        private static string connectionString;
        private static Networking server;

        /// <summary>
        /// Main process to start the server.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();

            builder.AddUserSecrets<WebServer>();
            IConfigurationRoot Configuration = builder.Build();
            var SelectedSecrets = Configuration.GetSection("WebServerSecrets");

            connectionString = new SqlConnectionStringBuilder()
            {
                DataSource = SelectedSecrets["ServerName"],
                InitialCatalog = SelectedSecrets["ServerDBName"],
                UserID = SelectedSecrets["ServerDBUsername"],
                Password = SelectedSecrets["ServerDBPassword"],
                ConnectTimeout = 15,
                Encrypt = false
            }.ConnectionString;

            server = new Networking(NullLogger.Instance, OnClientConnect, OnDisconnect, onMessage, '\n');

            Console.WriteLine("Connection established; waiting for requests.");
            server.WaitForClients(11001, true);

            while (true)
            {
                //Busy Loop Keeping Console Open
            }

        }

        /// <summary>
        /// Basic connect handler - i.e., a browser has connected!
        /// Print an information message
        /// </summary>
        /// <param name="channel"> the Networking connection</param>
        internal static void OnClientConnect(Networking channel)
        {
            Console.WriteLine("Client connected");

            channel.AwaitMessagesAsync();
        }

        /// <summary>
        ///   <para>
        ///     When a request comes in (from a browser) this method will
        ///     be called by the Networking code.  Each line of the HTTP request
        ///     will come as a separate message.  The "line" we are interested in
        ///     is a PUT or GET request.  
        ///   </para>
        ///   <para>
        ///     The following messages are actionable:
        ///   </para>
        ///   <para>
        ///      get highscore - respond with a highscore page
        ///   </para>
        ///   <para>
        ///      get favicon - don't do anything (we don't support this)
        ///   </para>
        ///   <para>
        ///      get scores/name - along with a name, respond with a list of scores for the particular user
        ///   <para>
        ///      get scores/name/highmass/highrank/startime/endtime - insert the appropriate data
        ///      into the database.
        ///   </para>
        ///   </para>
        ///   <para>
        ///     create - contact the DB and create the required tables and seed them with some dummy data
        ///   </para>
        ///   <para>
        ///     get index (or "", or "/") - send a happy home page back
        ///   </para>
        ///   <para>
        ///     get css/styles.css?v=1.0  - send your sites css file data back
        ///   </para>
        ///   <para>
        ///     otherwise send a page not found error
        ///   </para>
        ///   <para>
        ///     Warning: when you send a response, the web browser is going to expect the message to
        ///     be line by line (new line separated) but we use new line as a special character in our
        ///     networking object.  Thus, you have to send _every line of your response_ as a new Send message.
        ///   </para>
        /// </summary>
        /// <param name="network_message_state"> provided by the Networking code, contains socket and message</param>
        internal static void onMessage(Networking channel, string message)
        {
            string page;

            if (message.StartsWith("GET"))
            {
                string request = message.Substring(5);
                request = request.Split(" ")[0];
                request = request.Trim();

                Console.WriteLine("Request: " + request);

                if (request.StartsWith("highscore") || request.StartsWith("highscores"))
                {
                    // Respond with a highscore page
                    List<string> data = GetHighScores();

                    page = BuildHighscoresPage(data);
                    Console.WriteLine(page);
                    channel.Send(page);
                }
                else if (request.StartsWith("favicon"))
                {
                    // Don't do anything (we don't support this)
                }
                else if (request.StartsWith("scores"))
                {
                    string[] scoresArray = request.Split("/");
                    string name = scoresArray[1];

                    string info = "";

                    if (scoresArray.Length == 6)
                    {
                        //send the data to database
                        string highmass = scoresArray[2];
                        string highrank = scoresArray[3];

                        string starttime = scoresArray[4];
                        DateTime temp = (new DateTime(1970, 1, 1)).AddMilliseconds(double.Parse(starttime));
                        starttime = $"{temp.Year}/{temp.Month.ToString("00")}/{temp.Day.ToString("00")} {temp.Hour.ToString("00")}:{temp.Minute.ToString("00")}:{temp.Second.ToString("00")}";

                        string endtime = scoresArray[5];
                        temp = (new DateTime(1970, 1, 1)).AddMilliseconds(double.Parse(endtime));
                        endtime = $"{temp.Year}/{temp.Month.ToString("00")}/{temp.Day.ToString("00")} {temp.Hour.ToString("00")}:{temp.Minute.ToString("00")}:{temp.Second.ToString("00")}";

                        InsertGameInfo(name, starttime, endtime, highmass, highrank);

                        info = $"<p>Game data successfully saved in database. Here are the new game stats for {name}:</p>";
                    }

                    List<string> data = GetPlayerScores(name);

                    page = BuildPlayerScoresPage(name, data, info);
                    Console.WriteLine(page);
                    channel.Send(page);
                }
                else if (request.StartsWith("create"))
                {
                    List<string> data = GetHighScores();

                    page = BuildHighscoresPage(data, CreateDBTablesPage());
                    Console.WriteLine(page);
                    channel.Send(page);
                    
                }
                else if (request.StartsWith("index") || request.Equals("") || request.Equals("/") || request.Equals("index.html"))
                {
                    // Send a happy home page back
                    page = BuildMainPage();
                    Console.WriteLine(page);
                    channel.Send(page);
                }
                else if (request.StartsWith("css/styles.css?v=1.0"))
                {
                    // Send your site's CSS file data back
                    page = SendCSSResponse();
                    Console.WriteLine(page);
                    channel.Send(page);
                }
                else if (request.StartsWith("fancy"))
                {
                    List<string> data = GetPlayerPlayTimes();

                    page = BuildFancyPage(data);
                    Console.WriteLine(page);
                    channel.Send(page);
                } else
                {
                    page = BuildErrorPage();
                    Console.WriteLine(page);
                    channel.Send(page);
                }
            }
        }

        /// <summary>
        /// Create the HTTP response header, containing items such as
        /// the "HTTP/1.1 200 OK" line.
        ///
        /// See: https://www.tutorialspoint.com/http/http_responses.htm
        ///
        /// Warning, don't forget that there have to be new lines at the
        /// end of this message!
        /// </summary>
        /// <param name="length"> how big a message are we sending</param>
        /// <param name="type"> usually html, but could be css</param>
        /// <returns>returns a string with the response header</returns>
        private static string BuildHTTPResponseHeader(int length, string type = "text/html")
        {
            string header = "HTTP/1.1 200 OK \r\n" +
                "Date: " + DateTime.Now.Date.ToString() + "\r\n" +
                "Content-Length: " + length + "\r\n" +
                "Content-Type: " + type + "\r\n" +
                "Connection: close \r\n";

            return header;
        }

        /// <summary>
        /// Create a response message string to send back to the connecting
        /// program (i.e., the web browser).  The string is of the form:
        ///
        ///   HTTP Header
        ///   [new line]
        ///   HTTP Body
        ///  
        ///  The Header must follow the header protocol.
        ///  The body should follow the HTML doc protocol.
        /// </summary>
        /// <returns> the complete HTTP response</returns>
        private static string BuildMainPage()
        {
            string message = BuildHTTPBodyMain();
            string header = BuildHTTPResponseHeader(message.Length);

            return header + message;
        }

        /// <summary>
        ///   Create a web page!  The body of the returned message is the web page
        ///   "code" itself. Usually this would start with the doctype tag followed by the HTML element.  Take a look at:
        ///   https://www.sitepoint.com/a-basic-html5-template/
        /// </summary>
        /// <returns> A string the represents a web page.</returns>
        private static string BuildHTTPBodyMain()
        {
            return $@"
            <!DOCTYPE html>
            <html>
                <head>
                    <title>Agario</title>
                    <style>
                            h1, p {{
                                text-align:center;
                                font-family:verdana
                            }}
                            table, th, td {{
                                margin-left:auto;
                                margin-right: auto;
                                border:1px solid;
                                width: 500;
                                text-align:center;
                                font-family:verdana
                            }}
                        </style>
                </head>
                <body>
                    <h1>Agario</h1>
                    <p>Welcome to the Agario information database. Here is a list of supported requests:</p>
                    <table>
                        <tr>
                            <td><a href='http://localhost:11001/highscores'>High scores page</a></td>
                            <td>View a list of high scores for each player in the database.</a></td>
                        </tr>
                        <tr>
                            <td><a href='http://localhost:11001/scores/Draeden'>Player scores page</a></td>
                            <td>View a list of all recorded scores for a single player in the database.</a></td>
                        </tr>
                        <tr>
                            <td><a href='http://localhost:11001/create'>Create a new database</a></td>
                            <td>Create a new database if one does not already exist.</td>
                        </tr>
                        <tr>
                            <td><a href='http://localhost:11001/fancy'>Total Play Times</a></td>
                            <td>Shows the total minutes played by each player in the database.</td>
                        </tr>
                        <tr>
                            <td><a href='http://localhost:11001/scores/Draeden/5300/1/1682010830733/1682010843268'>Insert data</a></td>
                            <td>Insert new data about a game into the database.</td>
                        </tr>
                    </table>
                </body>
            </html>";
        }

        /// <summary>
        /// Returns the HTML code for the high score page. Functions the same as the main page method.
        /// </summary>
        private static string BuildHighscoresPage(List<string> data, string info = "")
        {
            string message = BuildHTTPBodyHighscores(data, info);
            string header = BuildHTTPResponseHeader(message.Length);

            return header + message;
        }

        /// <summary>
        /// Builds the HTML code for the high score page body.
        /// </summary>
        private static string BuildHTTPBodyHighscores(List<string> data, string info = "")
        {
            int rank = 1;

            string htmlPage = @$"
                <!DOCTYPE html>
                <html>
                    <head>
                        <title>High Scores</title>
                        <style>
                            h1, p {{
                                text-align:center;
                                font-family:verdana
                            }}
                            table, th, td {{
                                margin-left:auto;
                                margin-right: auto;
                                border:1px solid;
                                width: 500;
                                text-align:center;
                                font-family:verdana
                            }}
                        </style>
                    </head>
                    <body>
                        <h1>High Scores</h1>

                        {info}
                       
                        <table>
                            <tr>
                                <th></th>
                                <th> Player Name </th>
                                <th> High Score </th>
                            </tr>";

            foreach (string row in data)
            {
                string[] thisRow = row.Split(',');
                htmlPage += $@"
                            <tr> 
                                <td>{rank}</td>
                                <td>{thisRow[0]}</td>
                                <td>{thisRow[1]}</td>
                            </tr>";
                rank++;
            }

            htmlPage += @"
                        </table>
                    </body>
                </html>";

            return htmlPage;
        }

        /// <summary>
        /// Returns HTML code for the player score page. Functions the same as the main page method.
        /// </summary>
        private static string BuildPlayerScoresPage(string name, List<string> data, string info)
        {
            string message = BuildHTTPBodyPlayerScores(name, data, info);
            string header = BuildHTTPResponseHeader(message.Length);

            return header + message;
        }

        /// <summary>
        /// Builds the HTML code for the player score page body.
        /// </summary>
        private static string BuildHTTPBodyPlayerScores(string name, List<string> data, string info)
        {
            int rank = 1;

            string htmlPage = $@"
                <!DOCTYPE html>
                <html>
                    <head>
                        <title>Player High Scores</title>
                        <style>
                            h1, p {{
                                text-align:center;
                                font-family:verdana
                            }}
                            table, th, td {{
                                margin-left:auto;
                                margin-right: auto;
                                border:1px solid;
                                width: 500;
                                text-align:center;
                                font-family:verdana
                            }}
                        </style>
                    </head>
                    <body>
                        <h1>High Scores of {name}</h1>
                        
                        {info}
                       
                        <table>
                            <tr>
                                <th> </th>
                                <th> Max Mass </th>
                                <th> Rank </th>
                                <th> Start Time </th>
                                <th> End Time </th>
                            </tr>
                            </tr>";

            foreach (string row in data)
            {
                string[] thisRow = row.Split(',');
                htmlPage += $@"
                            <tr> 
                                <td>{rank}</td>
                                <td>{thisRow[0]}</td>
                                <td>{thisRow[1]}</td>
                                <td>{thisRow[2]}</td>
                                <td>{thisRow[3]}</td>
                            </tr>";
                rank++;
            }

            htmlPage += @"
                        </table>
                    </body>
                </html>";

            return htmlPage;
        }

        /// <summary>
        /// Returns HTML code for the total playtime page. Functions the same as the main page method.
        /// </summary>
        private static string BuildFancyPage(List<string> data)
        {
            string message = BuildHTTPBodyFancy(data);
            string header = BuildHTTPResponseHeader(message.Length);

            return header + message;
        }

        /// <summary>
        /// Builds the HTML code for the total playtime page body.
        /// </summary>
        private static string BuildHTTPBodyFancy(List<string> data)
        {
            int rank = 1;

            string htmlPage = $@"
                <!DOCTYPE html>
                <html>
                    <head>
                        <title>Total Play Times</title>
                        <style>
                            h1, p {{
                                text-align:center;
                                font-family:verdana
                            }}
                            table, th, td {{
                                margin-left:auto;
                                margin-right: auto;
                                border:1px solid;
                                width: 500;
                                text-align:center;
                                font-family:verdana
                            }}
                        </style>
                    </head>
                    <body>
                        <h1>Total Play Times</h1>
                       
                        <table>
                            <tr>
                                <th> </th>
                                <th> Player </th>
                                <th> Total Minutes Played </th>
                            </tr>
                            </tr>";

            foreach (string row in data)
            {
                string[] thisRow = row.Split(',');
                htmlPage += $@"
                            <tr> 
                                <td>{rank}</td>
                                <td>{thisRow[0]}</td>
                                <td>{thisRow[1]}</td>
                            </tr>";
                rank++;
            }

            htmlPage += @"
                        </table>
                    </body>
                </html>";

            return htmlPage;
        }

        /// <summary>
        /// Returns HTML code for an error page. Functions the same as the main page method.
        /// </summary>
        private static string BuildErrorPage()
        {
            string message = BuildHTTPBodyError();
            string header = BuildHTTPResponseHeader(message.Length);

            return header + message;
        }

        /// <summary>
        /// Builds the HTML code for the error page body.
        /// </summary>
        private static string BuildHTTPBodyError()
        {
            string htmlPage = $@"
                <!DOCTYPE html>
                <html>
                    <head>
                        <title>Total Play Times</title>
                        <style>
                            h1, p {{
                                text-align:center;
                                font-family:verdana
                            }}
                            table, th, td {{
                                margin-left:auto;
                                margin-right: auto;
                                border:1px solid;
                                width: 500;
                                text-align:center;
                                font-family:verdana
                            }}
                        </style>
                    </head>
                    <body>
                        <h1>404</h1>
                        <p>Error: Request not found</p>
                    </body>
                </html>";
                       
            return htmlPage;
        }

        /// <summary>
        /// SQL method for retrieving high score data for every player in the database.
        /// </summary>
        /// <returns>High score data from database.</returns>
        private static List<string> GetHighScores()
        {
            List<string> allRows = new();

            try
            {
                using SqlConnection con = new(connectionString);
                con.Open();

                using SqlCommand command = new SqlCommand("SELECT PlayerName, MAX(MaxMass) AS HighScore FROM Games GROUP BY PlayerName ORDER BY HighScore DESC;", con);
                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string row = $"{reader["PlayerName"]},{reader["HighScore"]}";
                    Console.WriteLine(row);

                    allRows.Add(row);
                }
            }
            catch (SqlException exception)
            {
                Console.WriteLine($"Error in SQL connection: {exception.Message}");
            }
            return allRows;
        }

        /// <summary>
        /// SQL method for retrieving every game data for a single player in the database.
        /// </summary>
        /// <returns>Player score data from database.</returns>
        private static List<string> GetPlayerScores(string playerName)
        {
            List<string> allRows = new();

            try
            {
                using SqlConnection con = new(connectionString);
                con.Open();

                using SqlCommand command = new SqlCommand($"SELECT MaxMass, MaxRank, StartTime, EndTime FROM GAMES WHERE PlayerName='{playerName}' ORDER BY MaxMass DESC;", con);
                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string row = $"{reader["MaxMass"]},{reader["MaxRank"]},{reader["StartTime"]},{reader["EndTime"]}";
                    Console.WriteLine(row);

                    allRows.Add(row);
                }
            }
            catch (SqlException exception)
            {
                Console.WriteLine($"Error in SQL connection: {exception.Message}");
            }
            return allRows;
        }

        /// <summary>
        /// SQL method for retrieving total playtime data for every player in the database.
        /// </summary>
        /// <returns>Total playtime data from database.</returns>
        private static List<string> GetPlayerPlayTimes()
        {
            List<string> allRows = new();

            try
            {
                using SqlConnection con = new(connectionString);
                con.Open();

                using SqlCommand command = new SqlCommand($"SELECT PlayerName, SUM(DATEDIFF(MINUTE,StartTime,EndTime)) AS TotalMinutes FROM Games GROUP BY PlayerName ORDER BY TotalMinutes DESC;", con);
                using SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string row = $"{reader["PlayerName"]},{reader["TotalMinutes"]}";
                    Console.WriteLine(row);

                    allRows.Add(row);
                }
            }
            catch (SqlException exception)
            {
                Console.WriteLine($"Error in SQL connection: {exception.Message}");
            }
            return allRows;
        }

        /// <summary>
        /// SQL method for inserting info for a new game into the database.
        /// </summary>
        private static void InsertGameInfo(string playerName, string startTime, string endTime, string maxMass, string highRank)
        {
            List<string> allRows = new();

            try
            {
                using SqlConnection con = new(connectionString);
                con.Open();
                
                using SqlCommand command = new SqlCommand($@"INSERT INTO dbo.Games (playerName, StartTime, EndTime, MaxMass, MaxRank)
                                                          VALUES ('{playerName}', '{startTime}', '{endTime}', {maxMass}, {highRank});", con);

                using SqlDataReader reader = command.ExecuteReader();

                reader.Read();
            }
            catch (SqlException exception)
            {
                Console.WriteLine($"Error in SQL connection: {exception.Message}");
            }
        }

        /// <summary>
        /// Handle some CSS to make our pages beautiful. Send back our CSS formatting.
        /// </summary>
        /// <returns>HTTP Response Header with CSS file contents added</returns>
        private static string SendCSSResponse()
        {
            string css = @"
            h1, p {
                    text-align:center;
                    font-family:verdana           
            }
            table, th, td {
                    margin-left:auto;
                    margin-right:auto;
                    border: 1px solid;
                    width: 500;
                    text-align:center;
                    font-family:verdana    
            }";
            string header = BuildHTTPResponseHeader(css.Length, "text/css");
            return header + css;
        }

        /// <summary>
        ///    (1) Instruct the DB to seed itself (build tables, add data)
        ///    (2) Report to the web browser on the success
        /// </summary>
        /// <returns> the HTTP response header followed by some informative information</returns>
        private static string CreateDBTablesPage()
        {
            List<string> allRows = new();

            try
            {
                using SqlConnection con = new(connectionString);
                con.Open();

                using SqlCommand command = new SqlCommand($@"IF NOT EXISTS(
                    SELECT * FROM sys.tables t
                    JOIN sys.schemas s ON(t.schema_id = s.schema_id)
                    WHERE s.name = 'dbo' AND t.name = 'Games')

                    BEGIN

                    CREATE TABLE dbo.Games(
                        GameID int NOT NULL IDENTITY(1, 1) PRIMARY KEY,
                        PlayerName varchar(50) NOT NULL,
                        StartTime datetime NOT NULL,
                        EndTime datetime NOT NULL,
                        MaxMass float NOT NULL,
                                MaxRank int NOT NULL
                                );

                INSERT INTO dbo.Games(PlayerName, StartTime, EndTime, MaxMass, MaxRank)
                    VALUES('Draeden', '2023-04-21 13:00:00.000', '2023-04-21 13:00:00.000', 1000, 1);

                INSERT INTO dbo.Games(PlayerName, StartTime, EndTime, MaxMass, MaxRank) 

                    VALUES('Jim', '2023-04-21 13:00:00.000', '2023-04-21 13:30:00.000', 1500, 1);

                INSERT INTO dbo.Games(PlayerName, StartTime, EndTime, MaxMass, MaxRank)

                    VALUES('Derek', '2023-04-21 13:00:00.000', '2023-04-21 13:26:00.000', 1750, 1);

                INSERT INTO dbo.Games(PlayerName, StartTime, EndTime, MaxMass, MaxRank)

                    VALUES('Draeden', '2023-04-21 13:00:00.000', '2023-04-21 13:45:00.000', 1100, 4);

                INSERT INTO dbo.Games(PlayerName, StartTime, EndTime, MaxMass, MaxRank)

                    VALUES('John', '2023-04-21 13:00:00.000', '2023-04-21 13:00:50.000', 500, 6);

                INSERT INTO dbo.Games(PlayerName, StartTime, EndTime, MaxMass, MaxRank)

                    VALUES('John', '2023-04-21 14:00:00.000', '2023-04-21 15:00:00.000', 600, 8)

                END

                ELSE
                
                BEGIN
                SELECT * FROM dbo.Games
                END;", con);

                using SqlDataReader reader = command.ExecuteReader();


                if (reader.Read())
                    return "<p> The table already existed in the database. Here is the high score info:</p>";
                else 
                    return "<p> Table created! Here is the new high score info:</p>"; 
            }
            catch (SqlException exception)
            {
                Console.WriteLine($"Error in SQL connection: {exception.Message}");
                return "<p> The table already existed in the database. Here is the high score info:</p>";
            }
        }

        /// <summary>
        /// Simple disconnect callback.
        /// </summary>
        internal static void OnDisconnect(Networking channel)
        {
            Debug.WriteLine("Goodbye.");
        }

    }
}