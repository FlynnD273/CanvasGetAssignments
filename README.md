# CanvasGetAssignments
A tool that uses the public Canvas API to create a checklist for all future assignments in the markdown format

Settings file format:
API Key: <Paste your Canvas API key here>
Output Path: <Path to the text file to output to>
Header: <The line of text to add the assignments after>

This will remove everything after the header in the file, and replace it with the assignments checklist.

You can add the `-s` or `--silent` flag to the command-line arguments to exit the program without waiting for user interaction.
