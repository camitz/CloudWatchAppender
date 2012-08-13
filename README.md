A CloudWatch appender for log4net. You can configure log4net to direct some of you log events to CloudWatch, making them show up as data points on a graph.

It's easy to configure in web.config or app.config. The following will divert all your logs events to CloudWatch. It will send a value of 1 with unit "Count". The default name and namespace is "CloudWatchAppender".

    <log4net>
        <appender name="CloudWatchAppender" type="CloudwatchAppender.CloudwatchAppender, CloudwatchAppender">
        </appender>


        <!-- The AWSSDK uses log4net too! This will filter out those messages. -->
        <logger name="Amazon">
          <level value="OFF" />
        </logger>

        <root>
          <appender-ref ref="CloudWatchAppender" />
        </root>
    </log4net>

You can specify alost everything as parameters to the appender, for instance:

    <appender name="CloudWatchAppender" type="CloudwatchAppender.CloudwatchAppender, CloudwatchAppender">
        <unit value="Milliseconds"/>
        <value value="20"/>
        <name value="ProcessingTime"/>
        <namespace value="MyApp/Processor"/>

        <!-- Not yet supported -->
        <dimension0 type="Amazon.CloudWatch.Model.Dimension">
        <name value="InstanceID"/>
        </dimension0>
    </appender>


The appender will also parse the log message, however, any parameters set in the config will always override. Here's an example.

    log.InfoFormat("A user upload a new photo of size Value: 2.5 Kilobytes");

Check out the following blog posts that seeded the project.

[A CloudWatch appender for log4net]("http://blog.simpletask.se/awscloudwatch-log4net-appender")
[Improvning the CloudWatch Appender]("http://blog.simpletask.se/improvning-cloudwatch-appender")