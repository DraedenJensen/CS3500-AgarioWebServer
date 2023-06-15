## Author/Date/Copyright/GitHub

**Authors:** Draeden Jensen and Derek Kober

**Date:** 2023-04-25

**Copyright:** Draeden Jensen and Derek Kober and CS3500 - This work may not be copied for use in Academic Coursework.

[WebServer Repo](https://github.com/uofu-cs3500-spring23/assignment-nine---web-server---sql-draedenandderek.git)
-Use the commit taken to by the link

[Updated Agario Repo](https://github.com/uofu-cs3500-spring23/assignment8agario-draeden.git)
-Use the commit taken to by the link

## Comments to Evaluators

There was difficulty implementing the CSS aspect of the project. We found that formatting only worked when the styles where directly in the html of each page, and that all suggestions on Piazza wouldn't work. 

## Time Tracking (Personal Software Practice)

**Estimate**: 15 hours

**Total**: 12 hours
	
| Date | Time | Contributors   | Task                                 |
|------|------|----------------|--------------------------------------|
| 4/20 | 1hr  | Draeden & Derek | Building project                     |
| 4/21 | 3hr  | Draeden & Derek | Setting up HTML for server front end |
| 4/21 | 3hr  | Draeden        | Implementing DB communication        |
| 4/24 | 3hr  | Draeden & Derek | Create fancy CSS & error page        |
| 4/25 | 1hr  | Draeden & Derek | Finishing up client database code    |
| 4/25 | 1hr  | Derek           | Finishing up documentation           |

We both feel that our time estimates have gotten better and more realistic over the course of the semester.
 
## Database Table Summary

The database consists of the following tables:
- **Games**: Stores all data used in the game, such as Game.ID, PlayerName, StartTime, EndTime, MaxMass, and MaxRank.
- **Players**: Stores list of players in the game.
- **Clans**: Non-standard data for clans in the game.
- **PlayersInClans**: Non-standard data of relationships between players and clans.

## Extent of work

In this project, we achieved several goals to create a functional and user-friendly web server for Agario player information:
- Developed the web server to serve and return a basic welcome page (index.html) upon user access.
- Implemented a feature that allows the web server to display a highscores chart on a dedicated webpage.
- Enabled the web server to receive and process requests to store highscores submitted by users.
- Added functionality to the web server to present a webpage that showcases various charts, graphs, or other visual representations of the data saved in the database.
- Ensured that the client-side code can effectively communicate with the database to store and retrieve information as needed.

## Partnership Information

Draeden and Derek worked together on this project:
- The majority of the code was developed through pair programming sessions.
- In seperate sessions, Draeden implemented the database connection code and Derek workd on the documentation aspect of the project.

## Branching

No branches were created, and therefore, no merging issues were encountered.

## Testing
Our testing process involved the following:
- Interactively tested the web server by accessing various web pages and monitoring their functionality.
- Watched the database information to verify that data was being stored and retrieved correctly during.
