# SSMSMint | SSMS 19, SSMS 20, SSMS 21, SSMS 22 Productivity Addin
An SSMS plugin that streamlines SQL development with workflow enhancements.

As a SQL developer, I've encountered many of the shortcomings and limitations of SSMS. Looking for an opportunity to practice C#, I decided to create a tool that addresses the issues I personally faced. 
I realize there may already be both free and paid tools out there that solve similar problems in one way or another. However, my main interest is in practicing C#, writing code, solving challenges on my own, and building something that helps me in everyday work. 
I’ll be happy if others also find this tool as useful as I do.

# Supports
* SSMS 19
* SSMS 20
* SSMS 21
* SSMS 22

# Main Features
**Hotkeys and other parameters are specified by default.**

* Code regions support: Automatically folds regions in the editor based on --#region and --#endregion comments when a window is opened.

  <img width="200" height="40" alt="image" src="https://github.com/user-attachments/assets/ea1ae008-70ec-4085-aacc-a4c4c6f960ac" />

* Regions refresh: Instantly update all code regions using Ctrl+K, Ctrl+O or via the context menu.

* SQL script generation: Generate the SQL script for the object under the caret in a new window using Ctrl+F11 or the context menu.

* Object Explorer navigation: Jump to the corresponding object in Object Explorer based on the object under the caret with Shift+F11 or the context menu.

* Result grid search: Search across all result grids using Ctrl+Alt+F or from the main menu.

  <img width="350" height="450" alt="image" src="https://github.com/user-attachments/assets/fb17d480-0b6a-4bc3-ada0-3be5781289c3" />

* Mixed language typo check: Detect adjacent Cyrillic and Latin characters when saving a document.

  <img width="280" height="450" alt="image" src="https://github.com/user-attachments/assets/29a06b60-6ed0-47c1-a485-446f950297a7" />

* A text marker indicating that a variable has been declared but not used.

  <img width="350" height="300" alt="image" src="https://github.com/user-attachments/assets/cd48ed9e-0a16-44bd-a492-0b995aea83a3" />

* A text marker indicating that a variable is being used but not declared.

  <img width="350" height="300" alt="image" src="https://github.com/user-attachments/assets/423576dc-a470-45ba-905d-e91691660011" />

* Ability to view cell contents as formatted JSON in a new window.

  <img width="600" height="300" alt="image" src="https://github.com/user-attachments/assets/46a719ac-ede3-4168-b782-9e78b65e8be2" />

* Copying selected or all headers from Results Grids.
  
  <img width="300" height="100" alt="image" src="https://github.com/user-attachments/assets/a4c5a11b-369a-4729-ab53-886b8d0e1a5b" />

* Marking and unmasking the beginning and end of a comment for a block of text. Works with -- and /*...*/ depending on the current state Ctrl+Num*.
  
  <img width="300" height="100" alt="image" src="https://github.com/user-attachments/assets/e053e836-97a7-43d0-a733-878739b39cfd" />



# Options
Options page at **Tools - Options - SSMSMint**

<img width="300" height="300" alt="image" src="https://github.com/user-attachments/assets/3e65e53a-3006-4890-ad5a-41145c725edf" />

# Installation
You have two options:
1. The build process will automatically copy all necessary files into the SSMS extension folder.
2. Unpack the archive from the [Releases](https://github.com/MatveevAleksandr/SSMSMint/releases) section to the Extensions folder of your SSMS installation (default paths below, or your custom installation path):
  * SSMS 19: C:\Program Files (x86)\Microsoft SQL Server Management Studio 19\Common7\IDE\Extensions
  * SSMS 20: C:\Program Files (x86)\Microsoft SQL Server Management Studio 20\Common7\IDE\Extensions
  * SSMS 21: C:\Program Files\Microsoft SQL Server Management Studio 21\Release\Common7\IDE\Extensions
  * SSMS 22: C:\Program Files\Microsoft SQL Server Management Studio 22\Release\Common7\IDE\Extensions

Restart SSMS after placing the files in the extension directory.

# Contributing
I will be glad to hear your ideas and suggestions. Also, if you encounter any errors, please send them to this repository.
* [Discussions](https://github.com/MatveevAleksandr/SSMSMint/discussions)
* [Issues](https://github.com/MatveevAleksandr/SSMSMint/issues)
