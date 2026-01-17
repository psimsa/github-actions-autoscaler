### overview
this is an old project of mine that i created to facilitate automated scaling of self hosted github runners on multiple servers and multiple personal repositories since github only provides group runners for organizaitons and not for personal repositories.

### the idea
basically, there are two parts of this solution:
- there is a rest endpoint that accepts webhook calls from github whenever a job is queued for a self hosted runner. it places the job in a queue, currently utilizing azure storage queues. there is a single instance of this endpoint used.
- there are runner coordinators that run on multiple servers. each coordinator monitors the queue for jobs, and when a job is found, it spins up a new self hosted runner in a docker container to pick up the job. once the job is complete, the runner is removed. 

both parts are a single .net project, started with different arguments.

you can see sample configuration files in the `sample-config` folder. it contains a docker compose folder which, when ran as-is, will start and register a runner coordinator with necessary settings to connect to the queue.

### status
so this thing works very well but is not very well structured, documented and maintained. i would like to revive it and add some new features but want to first modernize it. so here are the tasks:
- analyze the repository to gain complete, in-depth understanding of the current implementation and functionality
- create a plan that will restructure the repo to standard modern practices, include src/ and tests/ folders and upgrade the solution to .NET 10 and C# 14.
- execute the initial plan, documenting finished tasks and changes made
- update documentation to reflect the new structure 
- re-analyze the repository to identify areas for improvement, componentization, testability and optimizations
- create initial test coverage before further refactoring efforts
- plan and execute further refactoring, componentization, testability and optimizations as identified in the re-analysis phase. use the tests to ensure stability and correctness of the codebase throughout these changes. 
- once the solution is testable, plan and create comprehensive unit and integration tests to ensure code quality and reliability.

### notes
- use skills, tools and MCP servers to efficiently work with the code, look up documentation and test changes
- ensure that all changes are well documented, including code comments, README updates and any necessary documentation
- create and use docs/ folder to store any todo lists, temporary information and plans during the process
- create a new branch and make regular, well documented commits to track progress and changes made. do not push to upstream, only make local commit.