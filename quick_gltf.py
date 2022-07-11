bl_info = {
    'name': 'QuickGltf',
    'blender': (2, 80, 0),
    'category': 'Object',
}

keymaps = []

import bpy
import os

class QuickGltf(bpy.types.Operator):
    '''Export an object. If it's a curve converts it to a mesh first'''
    bl_idname = 'object.quick_gltf'
    bl_label = 'Quick GLTF'
    bl_options = {'REGISTER', 'UNDO'}

    def execute(self, context):
        all_selected = bpy.context.selected_objects

        if len(all_selected) == 0:
            self.report_warning('No objects selected')
            return {"CANCELLED"}

        for obj in all_selected:
            obj.select_set(True)
        bpy.ops.object.select_grouped(type='CHILDREN_RECURSIVE')

        all_selected = bpy.context.selected_objects

        blend_file_path = bpy.data.filepath
        directory = os.path.dirname(blend_file_path)
        target_path = os.path.join(directory, all_selected[0].name + '.glb')

        created_objects = []

        for obj in all_selected:
            cloned = self.clone_for_export(context, obj)
            cloned.location.x = 0
            cloned.location.y = 0
            cloned.location.z = 0
            created_objects.append(cloned)

        for obj in created_objects:
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
    
    def clone_for_export(self, context, obj):
        if obj.type == 'CURVE':
            return self.curve_to_mesh(context, obj)
        else:
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