ArmiTex
=======

ArmiTex is a litte and simple to use tool to establish a MySQL connection within LaTex.

**Features**
- Establish connections to MySQL Databases (`!!mysqlcon`)
- Single- and Multiline results (`!!mysqlsingle`, `!!mysqlquery`)
- Non-Queryable results (`!!mysqlexecute`)
- SQL result iteration (`!!mysqlforeach`)
- Nested LaTex file structures (`\include`)

# Usage
Write in terminal or console: `ArmiTex.exe <arguments>`

| Argument | Description | Mandatory |
|---	|---	|---	|
| -f<br/>--file | Specify the root .tex file to be parsed. | Yes |
| -h<br/>--help | Displays this help list. | No |
| -o<br/>--output  | Specify a directory where the parsed .tex file shall be generated. If not specified, output will be generated in a subfolder at the root file location. | No |

Example: `.\ArmiTex\bin\ArmiTex.exe -f .\Index.tex -o .\output`
See [example.tex](example.tex) for a detailed usage example within LaTex.

# Notes
Tested with Windows 10 Professional x64 and MySQL Server 8.0.21.
Tested with queries on tables, views and stored procedures.

This software was developed for a specific project and is only tested with the project. No warranties of any kind are given. You are free to adapt this work as specified in the [License](LICENSE) file - but please either provide credits or a reference to this project.