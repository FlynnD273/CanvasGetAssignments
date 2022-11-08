# CanvasGetAssignments
A tool that uses the public Canvas API to create a checklist for all future assignments in the markdown format

## Settings File

There is a file called settings.txt that is generated in the same directory as the exe file after the first time running the program. 
Change these lines to customize the behaviour of the program.  
API Key: `Paste your Canvas API key here`  
Output Path: `Path to the text file to output to`  
Weekly: `The line of text that starts the weekly tasks section. This should be before the Header line`
Header: `The line of text to add the assignments after`  
Time Zone: `The time zone to convert to (remove this line to use the local time zone)`

The weekly list must be before the assignments header. The weekly task section replaces all `[x]` with `[ ]` if the current day is Monday. 

Warning!  
This tool will remove everything after the header in the file, and replace it with the assignments checklist.

## Command-Line Arguments

| Flag | Usage |
| ---- | ----- |
| `-s`,`--silent` | When the program ends, exit without prompting for user interaction if no exception was thrown |
| `-se`, `--silentExceptions` | When the program ends, exit without prompting for user interaction even if an exception was thrown |


## To Build

This was made using Visual Studio 2022 and .NET 6.0. 
Make sure you have both installed when building from the source code.
