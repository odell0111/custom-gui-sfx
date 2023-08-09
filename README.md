# Custom GUI SFX - [Download Latest Release [951 KB]](https://github.com/odell0111/custom-gui-sfx/releases/download/latest/CustomGuiSfx-win64-n6.0-fd.zip)
Custom GUI SFX is a free and open source Windows tool to wrap an archive file with a size of ~4GB or less in a Windows executable along with custom information, some links, and images, so you will be able to share your files in a more beautiful and personalized way, in addition, you won't need to have any tool installed to unzip the archives because this program makes use of 7-Zip internally (www.7-zip.org)

## Screenshots
<div align="center">
	<img src="Screenshots/01.%20Screenshot.png"><br/><br/>
	<img src="Screenshots/02.%20Screenshot%20-%20Extracting.png"><br/><br/>
	<img src="Screenshots/03.%20GIF.gif?raw=true"> <br/><br/>
	<img src="Screenshots/04.%20Creation%20Progress%20Window.png"> <br/><br/>
</div>

## YouTube Video Tutorial
[Custom GUI Self-Extracting Archive - Share your files in a more beautiful and personalized way](https://www.youtube.com/watch?v=qlR_LbXq8Zo)

## Why does the archive size have to be ~4GB or less?
I mention the archive because it might be the largest file, but precisely the one that cannot be equal to or larger than 4GB is the final executable, the SFX…

(...) [The executable "image" (the code/data as loaded in memory) of a Win64 file is limited in size to 2GB. This is because the AMD64/EM64T processors use relative addressing for most instructions, and the relative address is kept in a dword. A dword is only capable of holding a relative value of ±2GB](http://www.godevtool.com/GoasmHelp/64bits.htm#diffe) (...)
...a signed dword has a range of ±2,147,483,647 and an unsigned dword has a range of 4,294,967,295 [4GB - 1B].

So, when you merge all of your source files (the archive, display images, and the configuration file) with the program executable, make sure that the final size of the executable is less than 4GB. This applies if you are creating your SFX from the command line. If you are creating it from the program, via the Send To context menu, an error will pop up and the SFX will not be created.

## Creating a Custom Graphical Self-Extracting archive
You have two ways of doing this.

The easiest one is to install the program and send the source files to the program as arguments via the Send To context menu, as shown in the [YouTube video tutorial](https://www.youtube.com/watch?v=qlR_LbXq8Zo). To install the program just open any custom GUI SFX executable, even if it has no archive merged with it, or open it with the switch -i.

The other one is to use command line utilities such as cat for Unix systems or copy for Windows.

- Example using cat:<br/>
	cat CustomGuiSfx.exe config.txt FirstDisplayImage.gif SecondDisplayImage.png MyArchive.7z > MySFX.exe
	
- Example using copy:<br/>
	copy /b CustomGuiSfx.exe + config.txt + MyArchive.7z MySFX.exe

When merging with any command line utility or any other method, keep in mind that:
* CustomGuiSfx.exe must be the first file to merge
* config.txt file must be the second one
* Display images, if any, must be the third one
* The archive file must be the last file to merge

## About the configuration file
The configuration file is the text file used to customize the SFX. If this file is not passed when creating the SFX via Send To context menu, the program will create a default one. However, if you are creating the SFX through command line utilities or any other method and forget to merge this file with the main executable, the SFX will throw an error when opening.

To create a configuration template, once the program is installed, open the File Explorer Context Menu and click on New Configuration Template. This action will generate a config.txt template file that will automatically open in your default text editor.

As of the latest release, the configuration file size must be less than or equal to 5KB.

* About configuration file entries:
	* All 'size' entries are in bytes
	* Any 'link' entry without its label entry will be ignored
	* To ignore an entry remove the = sign or set no value
	* FileToExecute requires a relative path to the file that you want to run after the extraction is done. The relative path should be based on the extraction path
	* Comment entry must be always the last entry
	* When creating the SFX via the Send To context menu, there's no need to specify any 'size' entry or ArchiveExtension entry. However, if you are creating the SFX using a command line utility or any other way, it is mandatory to specify the 'size' entries. The ArchiveExtension entry will not cause an error if it's not specified, but if the user chooses to extract the archive file, the program may not be able to recognize the file extension and, consequently, the extracted file may have no extension

## Command Line Feature - Uninstalling the program
At present, the command line feature is quite limited, but it is necessary for uninstalling the program. To do this, pass -u or --uninstall to any custom GUI SFX executable, even if it has no archive merged with it. Similarly, if you want to install the program, pass -i or --install. This can be useful if you want to perform a downgrade. Otherwise, it may not be necessary because the program can be installed by opening it normally.

<br/>
<div align="center">
	<img src="Screenshots/05.%20Command%20Line%20Feature.jpg"><br/><br/>
</div>

## Changing program settings
Program settings are located at “%AppData%/OGM/Custom GUI SFX/settings.json”. You can change them at any time using any text editor. If VerboseMessageBox is set to true and an error occurs during program execution, a more detailed message will be displayed. If you encounter any errors, please set this entry to true, reproduce the error again, take a screenshot of the error message displayed, and send it to me on my [Instagram](https://instagram.com/odell0111)

<br/>
<div align="center">
	<img src="Screenshots/06.%20Program%20Settings.jpg"><br/><br/>
</div>

## Notes for developers
Publish the project with the 'PublishProfile.pubxml' profile to get a single file, make sure to increase the project version.
