Phantom is a .NET build system written in C# and Boo.

For discussion, please use the mailing list:
http://groups.google.com/group/phantom-discuss

For complete documentation see the Phantom wiki:
http://wiki.github.com/JeremySkinner/Phantom

Posts about Phantom can be found on my blog:
http://www.jeremyskinner.co.uk

This project is licensed under the Microsoft Public License. 
http://www.microsoft.com/opensource/licenses.mspx

Example:

desc "Compiles the solution"
target compile:
  msbuild(file: "MySolution.sln", configuration: "release")

desc "Executes tests"
target test:
  nunit(assembly: "path/to/TestAssembly.dll")

desc "Copies the binaries to the 'build' directory"
target deploy:
	rmdir('build')
	
	with FileList("src/MyApp/bin/release"):
		.Include("*.{dll,exe}")
		.ForEach def(file):
			file.CopyToDirectory("build")
	
desc "Creates zip package"
target package:
  zip("build", 'build/MyApp.zip')


----------------
Acknowledgements
----------------
Many thanks to the following for their contributions to this project:

- Andrey Shchekin (http://www.ashmind.com)
- Ben Scheirman (http://flux88.com/)
- Emil Cardell (http://www.unwillingcoder.com)
- Mark Embling (http://www.markembling.info)
- Mikael Henrixon (http://blog.zoolutions.se)