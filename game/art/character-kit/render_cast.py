"""Original ART-20 clay bust kit. Blender 4.3+; no photos, external models or textures.

blender -b --python game/art/character-kit/render_cast.py -- [key ...]
The .blend source is this parameterized mesh construction, not a downloaded sculpt.
"""
import bpy
import math
import sys
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / "game/art/cast"
# skin, hair, clothing, coiffure, eyewear, beard, width, jaw, age, yaw, collar
# Authored against the scenario cast and established vector identities.
# Skin and hair colours are artistic choices, never surname/nationality rules.
# BUILD and HAIR_PART also keep repeated cuts from sharing one silhouette.
CAST = {
 "ruiz":      ("b98d6c","383a32","556e65","pony",False,False,.94,.86,38,-7,"uniform"),
 "cole":      ("dfb79a","895d45","657783","bun",True,False,.90,.84,40,6,"uniform"),
 "whitfield": ("956f56","b5b1a2","68796d","crop",False,True,1.13,1.13,57,-5,"stand"),
 "mills":     ("c99f7c","4c4339","7e8b71","curls",False,False,.94,.91,26,7,"uniform"),
 "hale":      ("c3977c","5e5a50","6b7864","recede",True,True,1.08,1.02,52,-3,"uniform"),
 "bell":      ("8d644e","2e3431","727d69","close",False,False,.97,1.18,34,5,"stand"),
 "park":      ("bfa084","494a43","536c71","sweep",True,False,1.04,1.07,37,-8,"uniform"),
 "nair":      ("b2876a","3e3733","73847c","bun",False,False,.95,.85,42,5,"uniform"),
 "delgado":   ("d3ab8d","875844","9d7759","curls-long",True,False,1.04,.94,45,-5,"jacket"),
 "mensah":    ("76543f","303632","718995","close",True,True,1.13,1.08,48,8,"uniform"),
 "okafor":    ("996e53","343c35","8b9173","coils",False,False,1.07,.92,31,-6,"crew"),
 "brandt":    ("e0ba9f","b7a179","687f6d","pixie",False,False,.90,.88,39,4,"uniform"),
 "lin":       ("c3a283","484237","66717d","bob",True,False,.90,.84,44,-5,"uniform"),
 "ortiz":     ("a98065","685245","8b8f74","bun",True,False,.99,.89,47,7,"uniform"),
 "dunn":      ("d0a28a","a7a18f","596e79","crop",False,True,1.06,1.15,61,-4,"stand"),
 "ward":      ("c7ae98","b0afa6","72836e","wave",True,False,.99,.88,59,6,"uniform"),
 "reyna":     ("c29371","584837","956d56","sweep",False,True,.94,1.03,46,-7,"polo"),
 "brooks":    ("ab7f61","473e37","7f747a","bob",False,False,.94,.83,35,6,"vneck"),
 "silva":     ("c99c78","414c47","5f7f86","curls",False,True,1.06,1.08,39,-6,"polo"),
 "haddad":    ("d4ac8c","3f3c37","5c7971","curls-long",True,False,.89,.84,33,5,"vneck"),
 "kowalski":  ("d9b49b","827b6c","737880","recede",False,True,1.15,1.17,60,-5,"jacket"),
 "tan":       ("9f755c","343c37","a08662","wave",False,False,1.03,.89,48,7,"cardigan"),
 "moreau":    ("815f48","b1aba0","7d8c78","recede",True,False,.97,1.14,58,-5,"polo"),
 "sato":      ("e1bfa6","775540","738391","pony",False,False,.88,.83,30,6,"vneck"),
 "pryce":     ("bd8b68","524437","92776a","bald",False,False,1.04,1.12,56,-7,"crew"),
 "carvalho":  ("916950","343b32","6f7d89","bob",True,False,1.00,.90,43,5,"cardigan"),
}

# Shoulder width, neck width: two underlying builds with individual variations.
BUILD = {
    "ruiz": (.77,.90), "cole": (.79,.88), "nair": (.78,.88),
    "ortiz": (.84,.96), "ward": (.82,.93), "brandt": (.76,.88),
    "lin": (.77,.87), "delgado": (.85,.94), "brooks": (.80,.90),
    "haddad": (.75,.86), "tan": (.83,.95), "sato": (.75,.87),
    "carvalho": (.84,.93), "bell": (.98,1.04), "okafor": (.86,.94),
    "kowalski": (1.08,1.08), "whitfield": (1.03,1.05),
}
HAIR_PART = {"ruiz": -1, "cole": 1, "nair": -1, "ortiz": 1,
             "lin": 1, "ward": -1, "haddad": -1, "tan": -1,
             "brooks": 1, "sato": 1, "carvalho": 1, "delgado": 1}


def material(name, code, value=1):
    mat = bpy.data.materials.new(name)
    rgb = [int(code[i:i+2],16)/255 for i in (0,2,4)]
    mat.diffuse_color = (*[(v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4)*value for v in rgb],1)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = mat.diffuse_color
    bsdf.inputs["Roughness"].default_value = .87
    bsdf.inputs["Specular IOR Level"].default_value = .18
    return mat


def mesh(name, verts, faces, mat, bevel=0, smooth=True):
    data=bpy.data.meshes.new(name); data.from_pydata(verts,[],faces); data.update()
    obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    if smooth:
        for poly in data.polygons: poly.use_smooth=True
    if bevel:
        mod=obj.modifiers.new("Soft sculpt edges","BEVEL");mod.width=bevel;mod.segments=3
        norm=obj.modifiers.new("Weighted normals","WEIGHTED_NORMAL")
    return obj


def ellipsoid(name, center, radii, mat, segments=32, rings=20, rot=0):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,location=center)
    obj=bpy.context.object;obj.name=name;obj.scale=radii;obj.rotation_euler[1]=math.radians(rot)
    obj.data.materials.append(mat)
    for poly in obj.data.polygons: poly.use_smooth=True
    return obj


def line(name, points, radius, mat, cyclic=False):
    curve=bpy.data.curves.new(name,"CURVE");curve.dimensions="3D";curve.resolution_u=12
    curve.bevel_depth=radius;curve.bevel_resolution=3
    spline=curve.splines.new("BEZIER");spline.bezier_points.add(len(points)-1)
    for point,co in zip(spline.bezier_points,points):
        point.co=co;point.handle_left_type="AUTO";point.handle_right_type="AUTO"
    spline.use_cyclic_u=cyclic
    obj=bpy.data.objects.new(name,curve);bpy.context.collection.objects.link(obj);obj.data.materials.append(mat)
    return obj


def lathe(name, profile, mat, n=48):
    verts=[]
    for z,rx,ry in profile:
        verts.extend((rx*math.sin(2*math.pi*j/n),-ry*math.cos(2*math.pi*j/n),z) for j in range(n))
    faces=[tuple(reversed(range(n)))]
    for i in range(len(profile)-1):
        faces.extend((i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j) for j in range(n))
    faces.append(tuple((len(profile)-1)*n+j for j in range(n)))
    return mesh(name,verts,faces,mat)


def face_surface(x,z,width):
    # Front of the same sculpt's eye/cheek plane. Kept shallow for inset features.
    return -.425*math.sqrt(max(.15,1-(x/(.52*width))**2))


def bust(key, spec):
    skin_code,hair_code,shirt_code,style,glasses,beard,width,jaw,age,yaw,collar=spec
    skin=material("Clay / skin",skin_code); shadow=material("Clay / warm recess",skin_code,.74)
    hair=material("Clay / hair",hair_code);cloth=material("Cloth",shirt_code)
    seam=material("Cloth seams",shirt_code,.74);paper=material("Natural linen","ddd8c5")
    ink=material("Graphite","303b36");eye=material("Ivory eyes","ece6d6")
    lip=material("Mouth",skin_code,.53)
    # Two shoulder builds and real neckline geometry, without marks or rank badges.
    shoulders,neck = BUILD.get(key, (.96,1))
    shoulder_profile=[(.30,.83,.32),(.52,.92,.36),(.85,.92,.35),
                      (1.08,.82,.31),(1.27,.56,.25),(1.40,.24,.21)]
    body=lathe("Shoulders",[(z,rx*shoulders,ry) for z,rx,ry in shoulder_profile],cloth)
    ellipsoid("Neck",(0,0,1.43),(.215*neck,.205,.40),skin)
    if collar in ("uniform","jacket","polo"):
        if collar in ("jacket","polo"):
            mesh("Open shirt inset",[(-.20,-.26,1.41),(.20,-.26,1.41),
                 (.08,-.36,1.01),(-.08,-.36,1.01)],[(0,1,2,3)],paper)
        for side in (-1,1):
            points=[(side*.19,-.235,1.48),(side*.37,-.295,1.27),
                    (side*.19,-.37,1.07),(side*.045,-.315,1.30)]
            mesh("Folded collar",points,[(0,1,2,3)],seam if collar=="jacket" else cloth,bevel=.018,smooth=False)
            line("Collar fold",[points[0],points[1],points[2]],.009,seam)
        line("Shirt placket",[(0,-.37,.33),(0,-.37,.86),(0,-.315,1.26)],.012,seam)
        if collar=="uniform":
            for side in (-1,1):
                line("Plain workshirt pocket",[(side*.29,-.347,1.04),
                     (side*.54,-.316,1.04)],.014,seam)
        if collar=="jacket":
            for side in (-1,1):
                mesh("Jacket lapel",[(side*.37,-.28,1.32),(side*.49,-.32,1.02),
                     (side*.14,-.375,.80),(side*.18,-.37,1.17)],[(0,1,2,3)],seam,bevel=.014)
    elif collar=="stand":
        lathe("Standing collar",[(1.30,.255,.24),(1.48,.23,.22)],seam)
        line("Zip",[(0,-.36,.60),(0,-.32,1.23),(0,-.24,1.46)],.014,paper)
    elif collar in ("vneck","cardigan"):
        mesh("V neckline",[(-.26,-.22,1.43),(.26,-.22,1.43),(0,-.365,1.06)],[(0,1,2)],skin if collar=="vneck" else paper)
        line("V binding",[(-.26,-.235,1.43),(0,-.38,1.06),(.26,-.235,1.43)],.026,seam)
        if collar=="cardigan":
            line("Cardigan opening",[(0,-.38,1.06),(0,-.37,.55)],.025,seam)
    else:
        points=[(.24*math.sin(t*math.pi/12),-.23*math.cos(t*math.pi/12),1.38-.06*math.cos(t*math.pi/12)) for t in range(24)]
        line("Crew neck binding",points,.038,seam,True)
    profile=[(1.48,.17*jaw,.20),(1.54,.29*jaw,.30),(1.69,.40*jaw,.365),(1.91,.48,.415),(2.15,.49,.425),(2.39,.475,.42),(2.57,.405,.365),(2.70,.27,.27),(2.76,.035,.04)]
    profile=[(z,rx*width,ry) for z,rx,ry in profile]
    lathe("Head sculpt",profile,skin)
    for side in (-1,1):
        ellipsoid("Ear",(side*.49*width,.005,2.04),(.10,.08,.18),skin)
        ellipsoid("Ear recess",(side*.53*width,-.056,2.04),(.037,.019,.087),shadow)
    if key=="fallback":return
    # Small eye openings with substantial skin lids; no glassy eyeballs or painted-on eyes.
    for side in (-1,1):
        x=side*.205*width;y=face_surface(x,2.13,width)
        ellipsoid("Eye socket",(x,y-.016,2.14),(.134,.033,.082),shadow)
        ellipsoid("Open eye",(x,y-.044,2.14),(.106,.033,.045),eye)
        ellipsoid("Iris",(x-side*.008,y-.073,2.14),(.038,.009,.039),ink)
        # Lid sculpt overlaps the edge of the eye, leaving a clean almond opening.
        line("Upper lid",[(x-.11,y-.042,2.14),(x,y-.068,2.185),(x+.11,y-.042,2.14)],.016,skin)
        line("Lower lid",[(x-.105,y-.041,2.13),(x,y-.065,2.099),(x+.105,y-.041,2.13)],.011,skin)
        tilt=.008 if side<0 else -.008
        line("Brow",[(x-.105,y-.021,2.255+tilt),(x,y-.045,2.275),(x+.105,y-.021,2.26-tilt)],.018 if shoulders<.86 else .026,hair)
        if age>=50:
            line("Eye fold",[(x-.10,y-.02,2.06),(x,y-.026,2.038),(x+.09,y-.01,2.06)],.007,shadow)
    mesh("Sculpted nose",[(-.064,-.413,2.25),(.064,-.413,2.25),(-.077,-.538,1.98),(.077,-.538,1.98),(0,-.593,2.02),(0,-.42,1.93)],[(0,2,4),(0,4,1),(1,4,3),(2,5,4),(3,4,5),(0,1,5,2),(1,3,5)],skin,bevel=.028)
    ellipsoid("Nose wing left",(-.075,-.46,1.985),(.05,.055,.041),skin)
    ellipsoid("Nose wing right",(.075,-.46,1.985),(.05,.055,.041),skin)
    if beard:
        # A shallow surface follows the actual jaw planes. No tubes around the face.
        verts=[];n=33;levels=7
        for k in range(levels):
            for j in range(n):
                a=-1.60+3.20*j/(n-1)
                side=abs(math.sin(a));z=(1.49+.15*side)*(1-k/(levels-1))+(1.70+.22*side)*(k/(levels-1))
                low,high=profile[0],profile[1]
                for lo,hi in zip(profile,profile[1:]):
                    if lo[0]<=z<=hi[0]:low,high=lo,hi;break
                t=max(0,min(1,(z-low[0])/(high[0]-low[0])))
                rx=low[1]+(high[1]-low[1])*t;ry=low[2]+(high[2]-low[2])*t
                verts.append(((rx+.018)*math.sin(a),-(ry+.018)*math.cos(a),z))
        faces=[]
        for k in range(levels-1):
            faces.extend((k*n+j,k*n+j+1,(k+1)*n+j+1,(k+1)*n+j) for j in range(n-1))
        mesh("Sculpted beard",verts,faces,hair)
    smile=.018 if age<50 else .009
    line("Mouth",[(-.13,-.383,1.79+smile),(0,-.414,1.775),(.13,-.383,1.79+smile)],.012,lip)
    ellipsoid("Lower lip",(0,-.397,1.744),(.10,.016,.018),skin)
    sculpt_hair(key, style, width, profile, hair)
    if glasses:
        for side in (-1,1):
            x=side*.205*width;y=face_surface(x,2.14,width)-.098
            if key in ("park","mensah","haddad","carvalho"):
                points=[(x+dx,y,2.14+dz) for dx,dz in [(-.13,-.04),(-.13,.06),(-.10,.08),(.10,.08),(.13,.06),(.13,-.04),(.10,-.07),(-.10,-.07)]]
            else:
                points=[(x+.135*math.cos(2*math.pi*j/16),y+.016*abs(math.cos(2*math.pi*j/16)),2.14+.09*math.sin(2*math.pi*j/16)) for j in range(16)]
            line("Open glasses frame",points,.014,ink,True)
            line("Glasses arm",[(side*.34*width,y,2.15),(side*.49*width,.025,2.14)],.012,ink)
        line("Glasses bridge",[(-.07,-.456,2.16),(0,-.48,2.18),(.07,-.456,2.16)],.012,ink)
    # Rotate the whole assembled sculpt; all features and clothes share this pose.
    for obj in list(bpy.context.scene.objects):
        if obj.type in ("MESH","CURVE"):
            obj.rotation_euler[2]+=math.radians(yaw)
            x,y=obj.location.x,obj.location.y;a=math.radians(yaw)
            obj.location.x=x*math.cos(a)-y*math.sin(a);obj.location.y=x*math.sin(a)+y*math.cos(a)


def sculpt_hair(key, style, width, profile, hair):
    part=HAIR_PART.get(key, -1)
    def section(z):
        for low,high in zip(profile,profile[1:]):
            if low[0]<=z<=high[0]:
                t=(z-low[0])/(high[0]-low[0])
                return low[1]+t*(high[1]-low[1]),low[2]+t*(high[2]-low[2])
        return .025,.025
    # No cap on a bare or receding crown. The scalp shell tapers into the forehead
    # and descends beside the ears; the front edge is never a level, thick brim.
    if style not in ("bald","recede"):
        verts=[]; n=64; levels=36
        for k in range(levels):
            v=k/(levels-1)
            for j in range(n):
                a=2*math.pi*j/n
                front=max(0, math.cos(a))
                edge=2.00+.43*front+.09*math.sin(a*part)*front
                edge+=.025*math.sin(5*a)+.012*math.cos(9*a)
                z=2.79+(edge-2.79)*v
                rx,ry=section(min(2.755,z))
                thickness=.008+(.030 if style=="close" else .055)*math.sin(math.pi*v)**.45
                verts.append(((rx+thickness)*math.sin(a),-(ry+thickness)*math.cos(a),z))
        faces=[]
        for k in range(levels-1):
            faces.extend((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j) for j in range(n))
        mesh("Tapered irregular hairline",verts,faces,hair)
        # Sideburns meet the ear, with a pointed end instead of a horizontal rim.
        for side in (-1,1):
            line("Temple lock",[(side*.40*width,-.23,2.42),
                 (side*.47*width,-.12,2.26),(side*.47*width,-.06,2.08)],.033,hair)
    if style in ("sweep","pixie","wave","bun","pony","bob"):
        # Flow from an offset part. The front locks overlap, break the contour,
        # and run diagonally into the sides rather than stacking on a cap.
        for i in range(5):
            x=part*(-.25+i*.115)*width
            z=2.70+.09*math.sin((i+1)*.55)
            lock=ellipsoid("Parted sculpted lock",(x,-.205,z),
                (.23 if style!="pixie" else .18,.20,.105),hair,rot=part*(24-i*4))
        line("Parted temple sweep",[(part*.23*width,-.30,2.68),
             (part*.39*width,-.29,2.49),(part*.46*width,-.16,2.28)],.060,hair)
    if style=="bun":
        # High and off centre so the bun is visible with a near-front camera.
        x=part*.31*width
        ellipsoid("Visible tied bun",(x,.11,2.83),(.235,.215,.235),hair)
        for j in range(5):
            a=2*math.pi*j/5
            ellipsoid("Bun fold",(x+.135*math.sin(a),-.015,2.83+.135*math.cos(a)),(.10,.065,.10),hair,segments=20,rings=12)
    if style=="pony":
        x=part*.49*width
        for j in range(4):
            ellipsoid("Ponytail fall",(x+part*.025*j,.15,2.38-j*.17),
                (.16-j*.015,.18,.23),hair,rot=part*-9,segments=24,rings=16)
    if style in ("curls","coils","curls-long"):
        large=style in ("coils","curls-long")
        for row in range(3):
            for j in range(11):
                a=2*math.pi*(j+.35*(row%2))/11
                r=(.135 if large else .10)
                z=2.69-row*.16+.035*math.sin(3*a)
                # Rows widen toward the temple; leave both brows uncovered.
                rx=(.29+row*.10)*width;ry=.27+row*.03
                ellipsoid("Sculpted curl",(rx*math.sin(a),-ry*math.cos(a),z),
                    (r,r,r*1.05),hair,segments=20,rings=12)
        ellipsoid("Curled crown",(0,0,2.74),(.33*width,.29,.16),hair)
        if style=="curls-long":
            for side in (-1,1):
                for j in range(5):
                    ellipsoid("Shoulder curl",(side*(.47+.025*math.sin(j))*width,.06,2.31-j*.16),
                        (.15,.22,.16),hair,segments=20,rings=12)
    if style in ("bob","wave"):
        for side in (-1,1):
            for j in range(3):
                # Behind the cheeks; longer than the jaw, distinct from a crop.
                ellipsoid("Parted bob fall",(side*(.43+.025*j)*width,.12,2.36-j*.24),
                    (.16,.28,.27 if style=="bob" else .24),hair,rot=side*(6+j*3))
        if style=="wave":
            for side in (-1,1):
                ellipsoid("Turned wave tip",(side*.50*width,-.01,1.82),(.19,.20,.12),hair,rot=side*-25)
    if style=="crop":
        for j in range(7):
            ellipsoid("Broken short crown",((j-3)*.095*width,-.04,2.71+.045*math.sin(j*1.8)),(.13,.26,.10),hair,rot=(j-3)*9)
    if style in ("recede","bald"):
        for side in (-1,1):
            ellipsoid("Temple hair",(side*.425*width,.115,2.29),(.085,.25,.29),hair)
        if style=="recede":
            ellipsoid("Receding back",(0,.22,2.56),(.37*width,.20,.17),hair)


def render(key):
    bpy.ops.object.select_all(action="SELECT");bpy.ops.object.delete(use_global=False)
    if key=="fallback":
        neutral=("8c958b","8c958b","8c958b","close",False,False,1,1,0,0,"crew")
        bust(key,neutral)
    else:bust(key,CAST[key])
    scene=bpy.context.scene
    scene.render.engine="CYCLES";scene.cycles.samples=128;scene.cycles.use_denoising=False
    scene.cycles.seed=20;scene.render.threads_mode="FIXED";scene.render.threads=4
    scene.render.resolution_x=256;scene.render.resolution_y=320;scene.render.resolution_percentage=100
    scene.render.film_transparent=True
    scene.world.use_nodes=True;scene.world.node_tree.nodes["Background"].inputs[0].default_value=(.75,.78,.72,1)
    scene.world.node_tree.nodes["Background"].inputs[1].default_value=.55
    bpy.ops.object.light_add(type="AREA",location=(-3.5,-4.5,6))
    light=bpy.context.object;light.name="Single soft north light";light.data.energy=450;light.data.shape="DISK";light.data.size=4
    light.rotation_euler=(Vector((0,0,1.6))-light.location).to_track_quat("-Z","Y").to_euler()
    bpy.ops.object.camera_add(location=(.5,-8,3.3))
    camera=bpy.context.object;camera.rotation_euler=(Vector((0,0,2.00))-camera.location).to_track_quat("-Z","Y").to_euler()
    camera.data.type="ORTHO";camera.data.ortho_scale=2.20;scene.camera=camera
    scene.view_settings.view_transform="Standard";scene.view_settings.look="Medium High Contrast"
    scene.render.image_settings.file_format="PNG";scene.render.image_settings.color_mode="RGBA"
    OUT.mkdir(parents=True,exist_ok=True);scene.render.filepath=str(OUT/(key+".png"))
    bpy.ops.render.render(write_still=True)
    print("CAST_RENDERED",key,flush=True)


if __name__=="__main__":
    args=sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    for key in args or [*CAST,"fallback"]:
        if key not in CAST and key!="fallback":raise ValueError(key)
        render(key)
