## 0.4 - In Progress
  * Created accompanying HTML docs site with [Jekyll](http://jekyllrb.com), hosted on [GitHub pages](https://pages.github.com/).
  * Added Topic ([#1](https://github.com/hhandoko/yam-dotnet/pull/1)) and Search client.
  * Added 'GetByGroup()' in Message client (undocumented).

## 0.3 - <small>2014/04/09</small>
  * Created YamNet.Client35 project to use in .Net 3.5 solutions. Using [RestSharp](http://restsharp.org/), [TaskParallelLibrary for .NET 3.5](http://www.nuget.org/packages/TaskParallelLibrary/1.0.2856), and [AsyncBridge](https://github.com/tejacques/AsyncBridge) to keep similar method signatures.
  * Updated all client's return void methods to return 'Task' instead so it can be awaited, instead of fire-and-forget. 

## 0.2 - <small>2014/04/04</small>
  * Added Message, Relationship, and Network client. Updated ConsoleTest project to provide some usage example.
  * Updated all client methods to async.

## 0.1 - <small>2013/08/15</small>
  * Initial release. Provides limited functionality to User client.