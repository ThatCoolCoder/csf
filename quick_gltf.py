bl_info = {
    'name': 'QuickGltf',
    'blender': (2, 80, 0),
    'category': 'Object',
}

keymaps = []

import bpy
import mathutils
import os

# Yes it's horribly messy, but it's a tricky task.

class QuickGltf(bpy.types.Operator):
    '''Exports all selected objects and their children into a GLTF file. Converts curves to meshes first'''

    bl_idname = 'object.quick_gltf'
    bl_label = 'Quick GLTF'
    bl_options = {'REGISTER', 'UNDO'}

    def execute(self, context):
        # Get selected stuff
        all_selected = bpy.context.selected_objects.copy()
        if len(all_selected) == 0:
            self.report_warning('No objects selected')
            return {"CANCELLED"}

        # Recursively select their children
        to_clone = []
        for obj in all_selected:
            to_clone += self.select_recursive(obj)

        # Figure out where to save
        blend_file_path = bpy.data.filepath
        directory = os.path.dirname(blend_file_path)
        target_path = os.path.join(directory, to_clone[0].name + '.glb')

        # Figure out which objects are "roots" and need to be moved to zero position after cloning is done
        # (Must not move the children as they're moved along with the parents)
        # (Has effect of recursion because we recursively select children earlier)
        roots = []
        for obj in to_clone:
            if obj.parent not in to_clone:
                roots.append(obj)

        # Clone/convert all objects
        created_objects = []
        old_name_to_cloned_name = {}
        new_roots = []
        for obj in to_clone:

            cloned = self.clone_for_export(context, obj)
            if obj in roots:
                new_roots.append(cloned)
            else:
                # Update parent of creatd object
                saved_matrix = cloned.matrix_world
                cloned.parent = bpy.data.objects[old_name_to_cloned_name[obj.parent.name]]
                cloned.matrix_world = saved_matrix

                
            created_objects.append(cloned)
            # Keep track of what this used to be for re-rooting
            old_name_to_cloned_name[obj.name] = cloned.name

        # Prepare objects for export
        for (idx, obj) in enumerate(created_objects):
            # Move roots to zero position so they're not offset in the exported file
            if obj in new_roots:
                obj.location = mathutils.Vector((0, 0, 0))
            # Select everything
            obj.select_set(True)

        bpy.ops.export_scene.gltf(filepath=target_path, use_selection=True)

        for obj in created_objects:
            bpy.ops.object.delete()

        return {'FINISHED'}
    
    # def execute(self, context):
    #     all_selected = bpy.context.selected_objects
    #     if len(all_selected) != 1:
    #         self.report_warning('Please select just one object')
    #         return {"CANCELLED"}

    #     selected = all_selected[0]

    #     blend_file_path = bpy.data.filepath
    #     directory = os.path.dirname(blend_file_path)
    #     target_path = os.path.join(directory, selected.name + '.glb')

    #     # Set to true if custom behaviour made a new object
    #     should_delete = False

    #     # Custom behaviours for different types of objects
    #     if selected.type == 'CURVE':
    #         selected = self.curve_to_mesh(context, selected)
    #         should_delete = True

    #     old_location = selected.location.copy()
    #     selected.location.x = 0
    #     selected.location.y = 0
    #     selected.location.z = 0

    #     bpy.ops.export_scene.gltf(filepath=target_path, use_selection=True)

    #     if should_delete:
    #         bpy.ops.object.delete()
    #     else:
    #         selected.location = old_location

    #     return {'FINISHED'}

    def pos_relative_to_root(self, obj, roots):
        current_obj = obj
        sub_total_position = mathutils.Vector((0, 0, 0))
        print('rooting', obj.name)
        while current_obj not in roots:
            print('sub: ', current_obj.matrix_local.translation)
            sub_total_position += current_obj.matrix_local.translation
            current_obj = current_obj.parent
        return sub_total_position
    
    def clone_for_export(self, context, obj):
        if obj.type == 'CURVE':
            return self.curve_to_mesh(context, obj)
        else:
            for already_selected in bpy.context.selected_objects:
                already_selected.select_set(False)
            obj.select_set(True)
            bpy.ops.object.duplicate()
            return context.selected_objects[0]
            
    def curve_to_mesh(self, context, curve):
        # https://blenderartists.org/t/alternative-to-bpy-ops-object-convert-target-mesh-command/1177790/3
        deg = context.evaluated_depsgraph_get()
        me = bpy.data.meshes.new_from_object(curve.evaluated_get(deg), depsgraph=deg)

        new_obj = bpy.data.objects.new(curve.name + "_mesh", me)
        context.collection.objects.link(new_obj)

        for o in context.selected_objects:
            o.select_set(False)

        new_obj.matrix_world = curve.matrix_world
        new_obj.select_set(True)
        context.view_layer.objects.active = new_obj

        return new_obj
    
    def select_recursive(self, obj):
        obj.select_set(True)
        result = [obj]
        for child in obj.children:
            result += self.select_recursive(child)
        return result

    def report_warning(self, message):
        self.report({"WARNING"}, message)
    
    def deselect_all(self, context):
        for selected_obj in context.selected_objects:
            selected_obj.select_set(False)

def menu_func(self, context):
    self.layout.operator(QuickGltf.bl_idname)
    
def register():
    bpy.utils.register_class(QuickGltf)
    bpy.types.VIEW3D_MT_object.append(menu_func)

    # handle the keymap
    wm = bpy.context.window_manager
    km = wm.keyconfigs.addon.keymaps.new(name='Object Mode', space_type='EMPTY')

    kmi = km.keymap_items.new(QuickGltf.bl_idname, 'F', 'PRESS', ctrl=True, shift=True)
    # kmi.properties.total = 4

    keymaps.append((km, kmi))

def unregister():
    bpy.utils.unregister_class(QuickGltf)

    for km, kmi in keymaps:
        km.keymap_items.remove(kmi)
    keymaps.clear()

if __name__ == '__main__':
    register()