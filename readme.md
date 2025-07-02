MongoDB appender for log4net
----------------------------

The title says it all. Check [Log4Net site](http://logging.apache.org/log4net/) or [MongoDB site](http://www.mongodb.org/) if you need more info.

This is the official .NET implementation for the [log4mongo](http://log4mongo.org) project

To get started, check out [@sammleach](https://twitter.com/sammleach) blog post: [.NET Logging with log4mongo-net](http://samlea.ch/dev/log4mongo-net/)

Installation
------------

[Get it on NuGet](https://nuget.org/packages/log4mongo-net), or download sources and run build.cmd to build

Appender configuration sample
-----------------------------

	<appender name="MongoDBAppender" type="Log4Mongo.MongoDBAppender, Log4Mongo">
	  <connectionString value="mongodb://127.0.0.1:27017/ProjectLogs?authSource=admin" />
	  <CollectionName value="pname%yyyyMMdd"/>
	  <expireAfterSeconds value="2592000" />
	  <field>
		<name value="timestamp" />
		<layout type="log4net.Layout.RawTimeStampLayout" />
	  </field>
	  <field>
		<name value="logtime" />
		<layout type="log4net.Layout.PatternLayout" value="%date{yyyy-MM-dd HH:mm:ss.fff}"/>
	  </field>
	  <field>
		<name value="level" />
		<layout type="log4net.Layout.PatternLayout" value="%level"/>
	  </field>
	  <field>
		<name value="processName" />
		<layout type="log4net.Layout.PatternLayout" value="%appdomain"/>
	  </field>
	  <field>
		<name value="hostname" />
		<layout type="log4net.Layout.PatternLayout" value="%property{log4net:HostName}"/>
	  </field>
	  <field>
		<name value="processId" />
		<layout type="log4net.Layout.PatternLayout" value="%property{ProcessId}"/>
	  </field>
	  <field>
		<name value="thread" />
		<layout type="log4net.Layout.PatternLayout" value="%thread"/>
	  </field>
	  <field>
		<name value="logger" />
		<layout type="log4net.Layout.PatternLayout" value="%logger"/>
	  </field>
	  <field>
		<name value="message" />
		<layout type="log4net.Layout.PatternLayout" value="%message"/>
	  </field>
	  <field>
		<name value="error" />
		<layout type="log4net.Layout.ExceptionLayout"/>
	  </field>
	</appender>

License
-------

[BSD 3](https://raw.github.com/log4mongo/log4mongo-net/master/LICENSE)

Credits
-------

Thanks to [JetBrains](http://www.jetbrains.com/) for providing us licenses for its excellent tool [ReSharper](http://www.jetbrains.com/resharper/)

![ReSharper](http://www.jetbrains.com/img/logos/logo_resharper_small.gif)
