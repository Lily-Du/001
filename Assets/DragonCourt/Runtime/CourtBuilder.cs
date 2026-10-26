using UnityEngine;

namespace DragonCourt
{
    public static class CourtBuilder
    {
        static Material stone, floor, roof, gold, dark, red;
        public static GameObject Build()
        {
            TextAsset data = Resources.Load<TextAsset>("WhiteboxLayout");
            if (data == null) throw new System.InvalidOperationException("WhiteboxLayout.json missing from Resources.");
            WhiteboxLayout layout = JsonUtility.FromJson<WhiteboxLayout>(data.text);
            stone = Material("Stone / solid occluder", new Color(.65f, .7f, .73f));
            floor = Material("Walkable ground", new Color(.32f, .38f, .4f));
            roof = Material("Reliable roof / blue", new Color(.24f, .46f, .57f));
            gold = Material("Goal / interactable", new Color(.95f, .7f, .2f));
            dark = Material("Dragon", new Color(.17f, .21f, .25f));
            red = Material("Player", new Color(.8f, .23f, .14f));
            GameObject root = new GameObject("DRAGON COURT - generated whitebox");
            Transform geometry = new GameObject("01 Geometry (1 unit = 1 meter)").transform;
            geometry.SetParent(root.transform);
            foreach (BlockSpec b in layout.blocks)
            {
                Material mat = b.kind == "floor" ? floor : b.kind == "roof" ? roof : b.kind == "goal" ? gold : stone;
                GameObject block = Primitive(b.name, PrimitiveType.Cube, geometry,
                    V(b.center), V(b.size), mat, true);
                block.isStatic = true;
            }
            foreach (RampSpec r in layout.ramps) MakeRamp(r, geometry);

            CourtFlow flow = new GameObject("02 Level flow").AddComponent<CourtFlow>();
            flow.transform.SetParent(root.transform);
            CourtPlayer player = MakePlayer(root.transform);
            flow.player = player;
            CourtDragon dragon = MakeDragon(root.transform);
            flow.dragon = dragon;
            dragon.player = player;
            dragon.flow = flow;

            flow.sword = Primitive("Sword - E to restore pact", PrimitiveType.Cube, root.transform,
                new Vector3(0, 1.3f, 166), new Vector3(.18f, 2.3f, .35f), gold, false).transform;
            Primitive("Sword crossguard", PrimitiveType.Cube, flow.sword,
                new Vector3(0, .55f, 0), new Vector3(5, .12f, 1), gold, false);
            flow.bell = Primitive("Bell - E to lure", PrimitiveType.Cylinder, root.transform,
                new Vector3(33, 9, 79), new Vector3(1.4f, .8f, 1.4f), gold, false).transform;
            flow.gate = Primitive("West gate - E to open", PrimitiveType.Cube, root.transform,
                new Vector3(-30, 2, 97), new Vector3(13.8f, 4, .5f), gold, true).transform;
            Primitive("Old pact mural - E to read", PrimitiveType.Cube, root.transform,
                new Vector3(-36.5f, 1.5f, 76), new Vector3(.15f, 2.5f, 3), gold, false);
            Primitive("Sword light landmark", PrimitiveType.Cylinder, root.transform,
                new Vector3(0, 16, 166), new Vector3(.4f, 15, .4f), gold, false);
            Label(root.transform, "A  /  PILGRIM PATH", new Vector3(0, 3, 15));
            Label(root.transform, "B  /  WATCH THE SKY", new Vector3(0, 5, 44));
            Label(root.transform, "C1  /  CHAPEL", new Vector3(-30, 5, 62));
            Label(root.transform, "C2  /  BELL TOWER", new Vector3(30, 13, 71));
            Label(root.transform, "D  /  ROYAL COURT", new Vector3(0, 8, 125));
            Label(root.transform, "E  /  BROKEN GALLERY", new Vector3(0, 6, 146));
            Label(root.transform, "F  /  RESTORE THE PACT", new Vector3(0, 5, 173));
            Label(root.transform, "E: mural", new Vector3(-34, 2.5f, 76));
            Label(root.transform, "E: bell", new Vector3(33, 11, 79));
            Label(root.transform, "E: gate", new Vector3(-30, 4.5f, 95));

            GameObject sun = new GameObject("Sun");
            sun.transform.SetParent(root.transform);
            sun.transform.rotation = Quaternion.Euler(36, -28, 0);
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            RenderSettings.ambientLight = new Color(.4f, .46f, .52f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.24f, .3f, .35f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .0025f;
            return root;
        }

        static CourtPlayer MakePlayer(Transform parent)
        {
            GameObject obj = new GameObject("Player (1.8m)");
            obj.transform.SetParent(parent);
            obj.transform.position = new Vector3(0, .1f, 4);
            obj.layer = 2; // Ignore Raycast: detection rays only query occluders.
            CharacterController cc = obj.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = .3f;
            cc.center = Vector3.up * .9f;
            cc.slopeLimit = 48f;
            cc.stepOffset = .3f;
            Primitive("Knight scale reference", PrimitiveType.Capsule, obj.transform,
                new Vector3(0, .9f, 0), new Vector3(.55f, .9f, .55f), red, false).layer = 2;
            CourtPlayer player = obj.AddComponent<CourtPlayer>();
            player.body = obj.transform.Find("Knight scale reference");
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent);
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.fieldOfView = 66;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 850;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.24f, .3f, .35f);
            player.view = camera;
            return player;
        }

        static CourtDragon MakeDragon(Transform parent)
        {
            GameObject obj = new GameObject("Dragon (42m wingspan)");
            obj.transform.SetParent(parent);
            obj.transform.position = new Vector3(28, 30, 56);
            CourtDragon dragon = obj.AddComponent<CourtDragon>();
            Primitive("Body", PrimitiveType.Sphere, obj.transform, Vector3.zero, new Vector3(4, 3.5f, 13), dark, false);
            Primitive("Tail", PrimitiveType.Cube, obj.transform, new Vector3(0, 0, -12), new Vector3(1.2f, 1.2f, 15), dark, false);
            Primitive("Neck", PrimitiveType.Cube, obj.transform, new Vector3(0, .7f, 7), new Vector3(2, 2, 5), dark, false);
            dragon.head = Primitive("Head / perception origin", PrimitiveType.Cube, obj.transform,
                new Vector3(0, .5f, 10), new Vector3(3, 2.6f, 4), dark, false).transform;
            dragon.leftWing = Wing(obj.transform, -1);
            dragon.rightWing = Wing(obj.transform, 1);
            Primitive("Left eye", PrimitiveType.Sphere, dragon.head,
                new Vector3(-.51f, .1f, .25f), new Vector3(.12f, .13f, .12f), gold, false);
            Primitive("Right eye", PrimitiveType.Sphere, dragon.head,
                new Vector3(.51f, .1f, .25f), new Vector3(.12f, .13f, .12f), gold, false);
            return dragon;
        }

        static Transform Wing(Transform parent, int side)
        {
            Transform pivot = new GameObject(side < 0 ? "Left wing pivot" : "Right wing pivot").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(side * 1.5f, 0, 1);
            Primitive("Wing silhouette", PrimitiveType.Cube, pivot,
                new Vector3(side * 9.5f, 0, -1), new Vector3(20, .35f, 7), dark, false);
            return pivot;
        }

        static void MakeRamp(RampSpec r, Transform parent)
        {
            float l = r.x - r.width / 2, h = r.x + r.width / 2;
            float bottom = Mathf.Min(r.y0, r.y1) - .5f;
            Mesh mesh = new Mesh();
            mesh.name = r.name;
            mesh.vertices = new[] {
                new Vector3(l,bottom,r.z0), new Vector3(h,bottom,r.z0),
                new Vector3(h,bottom,r.z1), new Vector3(l,bottom,r.z1),
                new Vector3(l,r.y0,r.z0), new Vector3(h,r.y0,r.z0),
                new Vector3(h,r.y1,r.z1), new Vector3(l,r.y1,r.z1)
            };
            mesh.triangles = new[] {4,7,6,4,6,5, 0,1,2,0,2,3, 0,4,5,0,5,1,
                3,2,6,3,6,7, 0,3,7,0,7,4, 1,5,6,1,6,2};
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            GameObject obj = new GameObject(r.name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = floor;
            obj.AddComponent<MeshCollider>().sharedMesh = mesh;
            obj.isStatic = true;
        }

        static void Label(Transform parent, string text, Vector3 position)
        {
            GameObject obj = new GameObject(text);
            obj.transform.SetParent(parent);
            obj.transform.position = position;
            TextMesh label = obj.AddComponent<TextMesh>();
            label.text = text;
            label.characterSize = .22f;
            label.fontSize = 64;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(1, .85f, .45f);
            obj.transform.rotation = Quaternion.identity;
        }

        static Vector3 V(float[] a) { return new Vector3(a[0], a[1], a[2]); }
        static Material Material(string name, Color color)
        {
            Material result = new Material(Shader.Find("Standard"));
            result.name = name;
            result.color = color;
            return result;
        }
        static GameObject Primitive(string name, PrimitiveType type, Transform parent,
            Vector3 position, Vector3 scale, Material material, bool solid)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid)
            {
                Collider collider = obj.GetComponent<Collider>();
                collider.enabled = false;
                if (Application.isPlaying) Object.Destroy(collider);
                else Object.DestroyImmediate(collider);
            }
            return obj;
        }
    }
}
