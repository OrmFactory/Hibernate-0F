## What is it?

Hibernate entity beans and repository generator plugin for [OrmFactory](https://ormfactory.com) database visual database model editor.

## How to use it?

- Download HibernateGenerator.dll from [Releases](https://github.com/OrmFactory/Hibernate-0F/releases)
- Put it into Plugins folder of OrmFactory
- Start OrmFactory
- Right-click on "Generators" into your project and select "Add generator"
- Select "Java entity generator for Hibernate ORM"

### Where I can find Plugins folder?

- Plugins folder you can find in %appdata%/OrmFactory path on windows
- MacOSX: /Users/%Name%/Library/Application Support/OrmFactory
- Linux: /home/%user%/.config/OrmFactory

## How to build it?

- clone this repository using `git clone --recurse-submodules https://github.com/OrmFactory/Hibernate-0F.git`
- if you forgot to add --recurse-submodules run `git submodule update --init` in cloned directory
- run `build.bat` or `build.sh`

## How to build my own generator?

First of all you need to create OrmFactory project. Create new connection, create or import database structure you want to use in source code generator.

Add new XML file generator to your project. Adjust partial generation to produce necessary tables and enter path for generated file. Run generator.

Replace entire body of DataStructure.xml with your generated xml.

Set "RunToTest" project as default to start. Build and run project.

RunToTest will run your plugin as OrmFactory will do. It's shorten test path while you debugging your own plugin.
