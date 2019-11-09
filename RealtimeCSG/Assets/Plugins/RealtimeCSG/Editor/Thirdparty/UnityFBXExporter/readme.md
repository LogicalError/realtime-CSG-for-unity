=========================
Unity FBX Exporter (1.1.1)
-------------------------

GitHub: https://github.com/KellanHiggins/UnityFBXExporter
Contact: unityfbxexporter@8bitgoose.com

The Unity FBX Exporter is a simple FBX writer designed to export static objects from Unity into the FBX format, preserving the materials, game object hierarchy and textures attached.

It was written for the Unity asset Building Crafter (http://u3d.as/ovC) which allows anyone using Unity to create buildings right in Unity without any modeling experience.

![Left is Unity](/Docs/ExampleExport.jpg?raw=true "Optional Title")


Features
-------------------------

1. Can export any GameObject into FBX format.

2. Supports FBX format 7.3, around 2013.

3. Exports materials into the FBX file.

4. Exports texture references into the FBX file.

5. Can make a copy of all materials and link them to newly minted FBX file.

6. Can make a copy of all textures and link them to newly create materials.

7. Export very deeply hierarchical Game Objects with just a few clicks.

8. Objects will export their rotation correctly throughout the hierarchy (thanks @quickfingerz on Github).

9. Objects will also export their scale correctly (thanks @quickfingerz on Github).


Known limitations
-------------------------

1. FBX format will only recognize diffuse maps and normal maps when exporting. Can not include height maps, for example.

2. Textures only support PBR Unity 5 shaders.

3. Only exports one UV map, not a AO UV 2 map.

4. Sort of works at Runtime. Needs to use File.IO instead of AssetDatabase to truly work at runtime.

5. Sometimes the reimported FBX files don't find the right materials. C'est la vie.

6. Exporting a prefab in the Project Tab sort of works. Needs more testing


Tutorial
-------------------------

It is very simple to use this exporter. You shouldn't have any problems, and if you do, please add an issue to the Github project

1. Select any GameObject in the scene.

2. Select the type of export you'd like

3. "Only GameObject" will export a new FBX but not create any new materials or textures and use the original as reference

4. "With new Materials" will export a new FBX and create new materials with the GameObject name + _ + material name

5. "With new Materials and Textures" does 2b plus copying textures to a new folder. This one takes a while. Be patient.

6. Wait a bit

7. If you've selected new textures, wait a bit longer. Copying and reimporting the textures takes Unity's brainpower.

8. Check the folder.

9. Usually the materials will align, but if you have a FBX in the root area it may create new materials instead of finding the old ones.

10. Success! You now have a brand new FBX file with everything parented correctly. Remember rotations in children still don't work.

NOTE: Sometimes the fbx file imports the materials as recursive instead of project wide for GameObject only export. If this happens, delete all the materials and reimport the FBX file using Project-Wide for material search.


Maya
------------------------

This was created without owning Maya, so please give feedback on how opening these FBX files work. You can give feedback at unityfbxexporter@8bitgoose.com.


Export to Blender
------------------------

Blender 2.70 doesn't take ASCII FBX files. So you'll need to download the converter from the FBX site. Then convert it to a binary file and then import it into blender. Because the relative texture names are correct, blender will import your albedo and normal texture. Pretty neat!

