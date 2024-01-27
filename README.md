# Rust Circuit Boss
> An Oxide plugin that converts Digital Logic Sim Diagrams to Circuitry Inside Rust.

This plugin comes in two parts. First you have the C# Plugins that are installed on your [Oxide Server](https://umod.org/documentation/getting-started), and a Digital Logic Sim project that comes with all the vanilla rust components built in the sim with them properly binded in the Plugin.

## How to Install
This is achieved by copying the contents of this repo's plugins folder into your plugins folder on your oxide server.
`<rust_server_data_dir>/oxide/plugins/*.cs`

`Picasso.cs` and `JData.cs` are required for `RustCircuitBoss.cs` to load and enable.

Picasso handles drawing to signs and JData is in charge of parsing and deploying [Digital Logic Sim (DLS)](https://sebastian.itch.io/digital-logic-sim) chips in game.

Next You must Copy the `Test` Folder from `./data/rust_circuit_boss` into your DLS files.

The Saves Location is Located at
`C:\Users\[USERNAME]\AppData\LocalLow\SebastianLague\Digital Logic Sim\V1\Projects`.
Copy the `Test` folder into there and open up the game.

## How to Build a Diagram
The current project files contain the basics for building Circuits in game such as
```
1. Electrical Branch (E_Branch)
2. Splitter
3. Memory Cell (Memory_Cell)
4. Etc.
```
You will also see other custom chips which are chips used in my 8 Bit Computer design based
off of [Ben Eater's Design](https://eater.net/8bit)

To build a Custom Chip (A chip containing Vanilla components), you must drag your version of `Test` (Or whatever you rename the project to) into your `<rust_server_data_dir>/oxide/data/rust_circuit_boss` folder and run the command `/c_load <project_name>` in game.

This will load the Project to your user account. Then you want to use the `/c_build <chip_name>` command to execute building the command. Defining `DEBUG` within `RustCircuitBoss` will auto load a project on plugin start to any account you specify by ID.
Check the Loaded() Hook for more information.

Running DEBUG Mode will also run the `/c_clear` command complimentary for you before spawning another custom chip. 

This is handy for Rapid Development.
