---
layout: default
title: Logging
nav_order: 4
parent: Concepts
---

🚀Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/jube-training) from the developer.

# Logging

Logging is provided by Log4Net which is a .Net port of the populate Log4J package.

The functionality of Log4Net is outside the scope of this document, yet it is sufficient to state that the overwhelming
majority of steps taken by the engine will write to a log buffer, which in turn will be streamed to persistence media
such as:

* Rotating Log Files in the local file system.
* Dispatch to Syslog Server (this works great and does not reduce response times too much).
* Storage in a Database (slowest and highest risk option).
* No storage (Errors should at least be logged, so this is inadvisable).

It seems atypical to configure a production system to not persist logging at INFO level, however in ultra-high volume
and load balanced environments this can be advisable, instead logging by sampling from just a single node in load
balancer rotation or by setting the trace querystring value in a transaction post (reserved for future use). In all
cases it is advisable to log at ERROR level.

Traditionally the logging is controlled via updating the contents of a file called Log4Net.config, which typically sits
in the same directory as the Jube binary executable. For Jube, as most modern containerised software, it is not
ideal to ship logging configurations as part of the binary executable, as it is brought together in a container build
process and limits the user in their configuration choices.

Jube supports approaches to Log4Net instantiation:

* Jube instantiation of ConsoleAppender and RollingFileAppender only based on the core appender values being passed in
  Environment
  Variables.
* Specification of a remote file location and log4net.config file, which is to say external the container and its
  volumes (i.e. a file system mount).

The following Environment Variables dictate the behaviour of Log4Net instantiation:

| Variable                      | Example                | Default Value                                                                                                      | Description                                                                                                                                                                                                                                                                                                                             | 
|-------------------------------|------------------------|--------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Log4NetConfigFileLocationName | /remote/log4net.config | Will fall back to programmatic instantiation if unavailable.                                                       | The XML configuration file allowing for advanced configuration and instantiation of the log4net logging library.  This is helpful in the case of configuration of remote logging such as syslog.  In the case of the RollingFileAppender all necessary settings are otherwise available via dedicated Environment Variables as follows. |
| Log4NetLogPath                | /remote/logs           | Will fall back to the binary directory as target for logs, which is far from ideal and may not work in containers. | The file path to write and rotate log files to,  given the absence of the Log4NetConfigFileLocationName environment variable                                                                                                                                                                                                            |
| Log4NetLogMaximumFileSize     | 500MB                  | 100MB                                                                                                              | The maximum file size before the logs get rotated to new files, given the absence of the Log4NetConfigFileLocationName environment variable                                                                                                                                                                                             |
| Log4NetLogMaxSizeRollBackups  | 10                     | 100                                                                                                                | The maximum number of files to write before they are purged, given the absence of the Log4NetConfigFileLocationName environment variable                                                                                                                                                                                                |
| Log4NetLogLevel               | INFO                   | ERROR                                                                                                              | The logging level to write log events out, given the absence of the Log4NetConfigFileLocationName environment variable                                                                                                                                                                                                                  |

# Logging Levels

Logging levels can be overwhelming in extremely high throughput environments, in which case it is
advisable to sample by tapping just a single node available to the load balancer. In all instance it is strongly
recommended to use a Syslog server rather than a store the logs locally so not to overwhelm the disk, while also
providing for improved security and centralised monitoring.

The logging levels have the following meaning in Jube:

| Level | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
|-------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| DEBUG | This logging level will produce an overwhelming amount of information.  The Debug logging level is intended for development and diagnosis of extremely complex issues and most typically in a local environment.  This logging level is so comprehensive that most lines of code executed will create a line of logging detailing what is happening, the value both before, after and a verbose description.  This level of logging should not be used unless directed by Jube Support. This logging level will cause an enormous amount of disk IO.                                                                                                                                                                                                                                                                                                                                                                                                               |
| INFO  | This logging level is intended to give a window into the real-time functioning of the platform and will included logging entries that are akin to transaction or event records.  There is a large amount of information available to this logging level and will allow for transactions to be inspected via the logs. In a very high throughput environment INFO logging can be overwhelming and it might be advisable to tap a single node of a load balancer for the purposes of sampling or use the trace querystring switch (reserved for future use). If building out a cluster of Jube instances which are load balanced,  it is often helpful to have one small node (with traffic in proportion) being directed to a node with INFO level enabled.  In this manner it is possible for an administrator to maintain the vast majority of transactions being processed without trace,  yet still have a sample of transaction trace to identify bottlenecks. |
| WARN  | This logging level relies on performance counters in the platform and is intended to write logs when these performance counters exceed a threshold as defined in the Thresholds and Limits section of this document.  This is extremely useful for identifying performance issues for intervention.  This logging level includes soft errors which are not runtime errors,  but have led to the fatal termination of a transaction while not having any further consequence beyond the scope of that transaction. WARN error messages are written out based on the platforms monitoring thresholds in the database table Warning_Thresholds.                                                                                                                                                                                                                                                                                                                       |
| ERROR | This logging level is the trapping and logging of any .Net runtime environment error.  Runtime errors are especially severe and will require immediate intervention, yet typically they should not lead to the termination of the Engine. This is the typical logging threshold for a production implementation,  with one load balanced node perhaps being set to INFO, as detailed above.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| FATAL | This logging level is the identification of a condition that leads to the termination of the application.  In general the Jube Engine is robust and there are no circumstances in code that would lead to the termination of the engine.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |

# Programmatic Instantiation

When running a .NET Docker image, the recommended approach for writing log files is to output logs to the standard
output (stdout) and standard error (stderr) streams, as Docker containers are designed to emit logs through these
streams by default.
This allows Docker's default logging driver, json-file, to capture and store the logs in JSON format on the host
machine. Henceforth, the ConsoleAppender is instantiated in all instances. It is also highly unlikely that a container
will have write permissions without convoluted and unsupported changes to either the Dockerfile or Docker Compose.

Given the absence of the Log4NetConfigFileLocationName environment variable, or failure to deserialize and instantiate
the logger with that configuration XML serialisation file data, the software will programmatically create a
RollingFileAppender taking its configuration parameters from the environment variables defaults above, or otherwise
specified.

Programmatic Instantiation can be taken to be the default.

# Log4Net XML Serialisation File

Jube can instantiate Log4Net in the traditional fashion via a file containing the Log4Net configuration
XML Serialisation:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<!-- This section contains the log4net configuration settings -->
<log4net>
    <appender name="Console" type="log4net.Appender.ConsoleAppender">
        <layout type="log4net.Layout.PatternLayout">
            <param name="ConversionPattern" value="%d %m %n"/>
        </layout>
    </appender>

    <appender name="RollingFileAppender" type="log4net.Appender.RollingFileAppender">
        <param name="File" value="Server.log"/>
        <param name="AppendToFile" value="true"/>
        <param name="RollingStyle" value="Size"/>
        <param name="MaxSizeRollBackups" value="10"/>
        <param name="MaximumFileSize" value="500MB"/>
        <param name="StaticLogFileName" value="true"/>
        <layout type="log4net.Layout.PatternLayout">
            <param name="Header" value="[Startup]&#13;&#10;"/>
            <param name="Footer" value="[Shutdown]&#13;&#10;"/>
            <param name="ConversionPattern" value="%d %m %n"/>
        </layout>
    </appender>

    <root>
        <level value="ERROR"/>
        <appender-ref ref="Console"/>
        <appender-ref ref="RollingFileAppender"/>
    </root>
</log4net>
```

Configure and place the above XML into a file in a remote location and proceed to specify the
Log4NetConfigFileLocationName environment variable. On application startup the file will be opened, parsed to XML and
then used to instantiate the logger. Should Log4Net XML Serialisation instantiation fail, it will fall back to software
instantiation.

There are a variety of options available to Log4Net, and it is advisable to read the documentation associated with that
project, yet most of the functionality is available should instantiation be via XML Serialisation file.