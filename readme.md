GitSourceMonitor
================

SourceMonitor is an interesting code metrics analyzer for several different programming languages. It is developed by Jim Wanner, and its homepage can be found at the [Campwood software website](http://www.campwoodsw.com/sourcemonitor.html).

This repository contains a simple tool written in C# that automates the generation of SourceMonitor project checkpoints for each commit in a git repository.

For an example, see <a href="https://github.com/juj/GitSourceMonitor/blob/master/example.png">a screenshot</a> of a SourceMonitor project generated using this tool from the [juj/MathGeoLib](https://github.com/juj/MathGeoLib) repository.

Usage
-----
To create a .smproj file containing the commits for your git repository, do the following:

<ol>
<li>Build the command-line tool. I provide .sln and .csproj for Visual Studio 2010. Setting up a build for Mono should be easy'ish, since the whole tool is only ~150 lines in a single file.</li>
<li>Add the executable to your path (or invoke using the full path on the command line).</li>
<li>Open up command line, and browse to the git repository you want to process.</li>
<li>Make sure you do not have any uncommitted modifications in the working tree, since the tool needs to check out each commit in turn!</li>
<li>Run GitSourceMonitor "branchname" "outputfilename.smproj", where branchname tells the git branch to process, and outputfilename.smproj specifies the SourceMonitor project file to output.</li>
<li>Done. After the tool finishes, you can open the .smproj file in SourceMonitor.</li>
</ol>

GitSourceMonitor can do incremental updates to the .smproj file, which means that after you make new commits to the repository, you can re-run the tool (step 5 with same input parameters) to incrementally add the new commits at the end of the project.

License
-------

The source code in this repository is released to public domain. Feel free to take it for whatever purposes.

The SourceMonitor tool is copyright of Campwood Software. See the [SourceMonitor website](http://www.campwoodsw.com/sourcemonitor.html) for its licensing information. 