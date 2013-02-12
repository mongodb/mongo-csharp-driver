C# Driver Version 1.7.1 Release Notes
=====================================

This is a minor release. The only change is to strictly enforce the
MaxConnectionIdleTime and MaxConnectionLifeTime settings. These used to be
loosely enforced, and connections were sometimes allowed to be used for a
bit longer. However, in certain environments (some firewalls, and in particular
in Windows Azure) connections that exceed the limits by even a little bit are
guaranteed to fail, so it is much safer to enforce these values strictly.

An online version of these release notes is available at:

https://github.com/mongodb/mongo-csharp-driver/blob/v1.7.1/Release%20Notes/Release%20Notes%20v1.7.1.md

File by file change logs are available at:

https://github.com/mongodb/mongo-csharp-driver/blob/v1.7.1/Release%20Notes/Change%20Log%20v1.7.1-Bson.txt
https://github.com/mongodb/mongo-csharp-driver/blob/v1.7.1/Release%20Notes/Change%20Log%20v1.7.1-Driver.txt

The full list of JIRA issues resolved in this release is available at:

https://jira.mongodb.org/secure/IssueNavigator.jspa?mode=hide&requestId=13109

Documentation on the C# driver can be found at:

http://www.mongodb.org/display/DOCS/CSharp+Language+Center
http://api.mongodb.org/csharp/current/
