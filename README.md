# CanvasGetAssignments
A tool that uses the public Canvas API to create a checklist for all future assignments in the markdown format

This project was made primarily so that I would have an easy-to-access offline todo list of all of my class assignments. I had to learn how to use the Canvas API, and I had to learn how to parse the resultant JSON files into something useful. Once I've received all of the data from Canvas that I need, I then use Linq to filter the collections into the various categories. I need to group the assignments by the class they're from, and sort them by due date if there is a due date. If there's no due date, then those assignments get sent to a different collection. All of these then get written into a markdown file as a checklist, with the due date and Canvas links to the assignments. If an assignment is completed on Canvas or checked off locally, it will remove that assignment from the list. I also implemented a weekly todo list that resets every Monday for more customization. This project is meant to integrate with Obsidian Notes, which is why I chose the markdown format. 

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

## To Build

This was made using Visual Studio 2022 and .NET 6.0. 
Make sure you have both installed when building from the source code.
