A CloudWatch appender for [log4net](http://logging.apache.org/log4net/ "log4net"). You can configure log4net to direct some of you log events to [CloudWatch](http://aws.amazon.com/CloudWatch/ "CloudWatch"), making them show up as data points on a graph.

All posts are made asynchronously to CloudWatch via the [AWSSDK](http://aws.amazon.com/sdkfornet/) library for .NET.

# Installation

To add CloudWatchAppender to your Visual Studio (>2010) project, run the following command in <a href="http://docs.nuget.org/docs/start-here/using-the-package-manager-console">Package Manager Console</a> or download the source from [GitHub](https://github.com/camitz/CloudWatchAppender).

<div class="nuget-badge">
<p>
<code>PM&gt; Install-Package CloudWatchAppender</code>
</p>
</div>

# Configuration

CloudWatchAppender is easy to configure in web.config or app.config. If you've been using log4net you, probably already have the section and some of the elements defined. The following will divert all your logs events to CloudWatch. It will send a value of 1 with unit "Count". The default MetricName and Namespace is "CloudWatchAppender" both.

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

CloudWatchAppender can do more than this. Pretty much anything you can post to CloudWatch via the AWSSDK is supported by the appender in different ways. A notable exception is timestamp which will of course be supported in the near future.

To change the default behavior, you can either provide the info in the config-file or as part of the log event message. The former is cleaner in that the code can remain agnostic to the log end point. The latter provides more power and granularity over what is posted. Other appenders will simply output the information as it is written.

**Warning** Make sure you read about CloudWatch [pricing](http://docs.amazonwebservices.com/AmazonCloudWatch/latest/DeveloperGuide/cloudwatch_concepts.html) so you'll not get any surprises. Particularly, CloudWatch treats each combination of name, namespace and dimension as a different custom metric. You have just 10 free custom metrics. After that they start charging you.

## Config-file

The following example will post a metric with [unit](http://docs.amazonwebservices.com/AmazonCloudWatch/latest/APIReference/API_MetricDatum.html) Milliseconds and the value 20 for all loggers using this appender. The metric name will be ProcessingTime and the namespace MyPadd/Process.

    <appender name="CloudWatchAppender" type="CloudwatchAppender.CloudwatchAppender, CloudwatchAppender">
        <unit value="Milliseconds"/>
        <value value="20"/>
        <name value="ProcessingTime"/>
        <namespace value="MyApp/Processor"/>

        <dimension0 type="Amazon.CloudWatch.Model.Dimension">
            <name value="InstanceID"/>
            <value value="%instanceid"/>
        </dimension0>
    </appender>

Notice also we've provided the first of ten possible ["dimensions"](#dimensions). You can specify any string you like for name and value of the dimension. Above, however, we're using a special token for the value, "%instanceid". This will be translated to the EC2 instance ID, see [Instance ID](#insanceid) below. In fact, any token recognized by any regular PatternLayout conversion [patterns](http://logging.apache.org/log4net/release/sdk/log4net.Layout.PatternLayout.html), for instance %logger, will be translated as such.

The exact same can be accomplished by using the ["PatternLayout"](#patternlayout) we provide and using the format rules outlined below to format the input string to the appender.

      <layout type="CloudWatchAppender.PatternLayout, CloudWatchAppender">
        <conversionPattern value="%message Value: 20 Milliseconds, Name: ProcessingTime, NameSpace: MyApp/Processor, Dimension0: InstanceID: %instanceid"/>
      </layout>

## Event log message

If you pass a string to the logger like this

    ILog log = LogManager.GetLogger(typeof(MyClass));
    log.Info("This part will be ignored. Value: 20 Milliseconds, Name: ProcessingTime this will be ignored too NameSpace: MyApp/Processor, Dimension0: InstanceID: %instanceid");

most of it will be ignored by the CloudWatchAppender. Of course, if there are other appenders listening on the logger, they will handle the string in their way. Most will output the entire string to whatever end point they are designed for.

The above message to the logger will behave in the same way as the previous examples. The CloudWatchAppender event parser will look for recognizable tokens, largely corresponding to the entities and units familiar to CloudWatch. The parser is pretty lenient in what is allowed but also let's unexpected input slip without warning. As the messages get complicated, especially if PatternLayout is used, it is important to understand the conflict resolution [rules](#conflicts).

Note that if the PatternLayout is used, either the original one or the one provided by CloudWatchAppender, the pattern %message or %m (deprecated) must be present in the conversion pattern for the event log message to be observed.

A list of [tokens](#tokens) supported by the event parser can be found below. 

## <a id="configoverrides"></a>ConfigOverrides

Normally input given in parameters to the appenders will override anything given in the log event message, including anything given in the conversion pattern. To change this behaviour, add

    <configOverrides value="false"/>

to the appender definition. Now, parameters to the appender will act as defaults, should the values be missing from the message.

# Statistics

As well as singles values with unit, CloudWatchAppender supports statistics. This is useful when sending single point values becomes too burdensome. Instead, you aggregate your data to be sent every minute or so. The CloudWatchAppender doesn't go the aggregation for you, you'll have to take care of that yourself. Todo? ;).

Statistics are posted by specifying Minimum, Maximum, Sum and SampleCount. (The average is calculated by CloudWatch as Sum/SampleCount.) These entities are all recognized by CloudWatchAppender and used as above, either in the event log message or in the config file.

The following behave the same way.

    <unit value="Milliseconds"/>
    <sum value="3000"/>
    <minimum value="3"/>
    <minimum value="400"/>
    <samplecount value="250"/>

    log.Info("unit: milliseconds, sum: 3000, minimum: 3, minimum: 400, samplecount: 250");
    

# <a id="dimensions"></a>Dimensions

CloudWatch supports up to 10 dimensions given as name/value pair. CloudWatchAppender has no limit but don't try exceeding 10.

**Warning** Again, make sure you understand that overusing dimensions can quickly become expensive.

In your config file under the appender element you can add dimensions simply by listing a bunch of elements like this:

      <dimension type="Amazon.CloudWatch.Model.Dimension">
        <name value="InstanceID"/>
        <value value="%instanceid"/>
      </dimension>

      <dimension type="Amazon.CloudWatch.Model.Dimension">
        <name value="Fruit"/>
        <value value="Apple"/>
      </dimension>

Again, note the pattern [%instanceid](#instanceid).

The corresponding equivalent pattern/event log message would be

    "Dimension: InstanceID: %instanceid, Dimension: Fruit: Apple" //(%instanceid only parsed if in a layout conversion pattern)

or simply

    "Dimensions: (InstanceID: %instanceid, Fruit: Apple)" //(%instanceid only parsed if in a layout conversion pattern)

Deppending on the setting of [ConfigOverrides](#configoverrides) individual dimensions compete according to name (key). Internally in CloudWatchAppender the dimensions are stores as a dictionary.

# <a id="patternlayout"></a> Using layouts and the CloudWatchAppender PatternLayout

The CloudWatchAppender supports layouts as expected. The most commonly used by far is PatternLayout. You can use the original PatternLayout but it is recommended to use the one provided in the CloudWatchAppender library.

The CloudWatchAppender PatternLayout subclasses PatternLayout so all features should work as expected. Plus there are a few extran functions. Any [patterns](http://logging.apache.org/log4net/release/sdk/log4net.Layout.PatternLayout.html) supported by PatternLayout is headed. Some of them, however, will be less sensible to use in this context. Firstly, some of them take to long and secondly, some will not make sense to CloudWatch or the appender. 

*Most conversion patterns have yet to be tested.*

## <a id="instanceid"></a> The instance ID converter

The pattern %instanceid anywhere in your conversion pattern will be replaced by the intance id of the EC2 instance on which the application is running. This information is retrieved via an AWS metadata API (synchronous) call at first use and then cached in a static field.

Providing the same token, or any other supported token for that matter, as a parameter to the appender in the config-file is also possible and works as expected.

## Added %logger functionality

The log4net PatternLayout provides the %logger or %c pattern, which translates to the name of the logger, typically the qualified name of the type issuing the event. You can also specify a precision specifier as a integer enclosed in curly brackets following the pattern, to filter out the end part of the name. 

CloudWathcAppender adds support for negative precision specifiers, filtering out the beginning part of the name. This is useful for providing the type and namespace as metric name and namespace respectively to CloudWatch.

Typically you'd create the logger like so.

    ILog log = LogManager.GetLogger(typeof(MyClass));

If the namespace is MyApp.MyNamespace, then *Name: %logger{2}* in your pattern would post "MyNamespace.MyClass" as the metric name to CloudWatch. The following may be more suitable for your needs.

    Name: %logger{1}, NameSpace: %logger{-1}

The negative precision specifier removes the last word from the name. The metric name will now by MyClass and the namespace will be MyApp.MyNamespace.

Dots (.) are converted to slashes (/) before sending. This is true of all strings passed as namespace anywhere.

## <a id="conflicts"></a> Handling conflicting input

TODO

# <a id="tokens"></a> Tokens recognized by CloudWatchAppender

TODO

Check out the following blog posts that seeded the project.

[A CloudWatch appender for log4net](http://blog.simpletask.se/awscloudwatch-log4net-appender)

[Improving the CloudWatch Appender](http://blog.simpletask.se/improving-cloudwatch-appender)
