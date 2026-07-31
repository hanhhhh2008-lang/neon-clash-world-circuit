using System.Collections.Generic;
using UnityEngine;

namespace NeonClash
{
    /// <summary>
    /// Project-owned articulated fighter overlay.  It uses Unity's built-in primitive
    /// meshes only; no downloaded character package or external model is required.
    /// The pose is driven from the same simulation tick as FighterPresentation, so
    /// this is visual feedback rather than a second gameplay implementation.
    /// </summary>
    public sealed class FighterMeshRigPresentation
    {
        private sealed class Part
        {
            public readonly Transform Transform;
            public readonly MeshRenderer Renderer;
            public readonly Color BaseColor;

            public Part(Transform transform, MeshRenderer renderer, Color baseColor)
            {
                Transform = transform;
                Renderer = renderer;
                BaseColor = baseColor;
            }
        }

        private readonly Transform root;
        private readonly Transform body;
        private readonly List<Part> parts = new List<Part>();
        private readonly Part torso;
        private readonly Part head;
        private readonly Part chest;
        private readonly Part rearUpperArm;
        private readonly Part rearForearm;
        private readonly Part rearThigh;
        private readonly Part rearCalf;
        private readonly Part frontUpperArm;
        private readonly Part frontForearm;
        private readonly Part frontThigh;
        private readonly Part frontCalf;

        private readonly float width;
        private readonly float height;
        private readonly Color primary;
        private readonly Color secondary;
        private readonly Color fabric;
        private readonly Color skin;
        private bool flash;

        private FighterMeshRigPresentation(Transform parent, FighterDefinition fighter, Color primaryColor,
            Color secondaryColor, Color fabricColor, Color skinColor)
        {
            primary = primaryColor;
            secondary = secondaryColor;
            fabric = fabricColor;
            skin = skinColor;
            width = fighter.BodyBuild == FighterBodyBuild.Agile ? 0.88f :
                fighter.BodyBuild == FighterBodyBuild.Power ? 1.15f :
                fighter.BodyBuild == FighterBodyBuild.Armored ? 1.20f : 1f;
            height = fighter.BodyBuild == FighterBodyBuild.Power ? 1.04f : 1f;

            root = new GameObject("Articulated 3D Fighter Rig").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0f, 0f, -0.22f);

            body = new GameObject("Mesh Bones").transform;
            body.SetParent(root, false);

            // A small local key light lets the built-in meshes read as volume in the
            // orthographic arena without affecting the painted stage or UI.
            Light keyLight = root.gameObject.AddComponent<Light>();
            keyLight.type = LightType.Point;
            keyLight.color = Color.Lerp(Color.white, secondaryColor, 0.18f);
            keyLight.intensity = 2.3f;
            keyLight.range = 7f;
            keyLight.shadows = LightShadows.None;
            keyLight.transform.localPosition = new Vector3(-0.8f, 3.3f, -2.7f);

            torso = Primitive("Torso", PrimitiveType.Capsule, body, primary, new Vector3(0.72f * width, 0.86f * height, 0.28f));
            head = Primitive("Head", PrimitiveType.Sphere, body, skin, new Vector3(0.48f * width, 0.52f * height, 0.34f));
            chest = Primitive("Chest Emblem", PrimitiveType.Sphere, body, secondary, new Vector3(0.13f, 0.13f, 0.10f));

            rearUpperArm = Primitive("Rear Upper Arm", body, fabric, 0.34f * width);
            rearForearm = Primitive("Rear Forearm", body, primary, 0.29f * width);
            frontUpperArm = Primitive("Front Upper Arm", body, fabric, 0.37f * width);
            frontForearm = Primitive("Front Forearm", body, primary, 0.32f * width);
            rearThigh = Primitive("Rear Thigh", body, fabric, 0.43f * width);
            rearCalf = Primitive("Rear Calf", body, primary, 0.35f * width);
            frontThigh = Primitive("Front Thigh", body, fabric, 0.46f * width);
            frontCalf = Primitive("Front Calf", body, primary, 0.37f * width);
        }

        public static FighterMeshRigPresentation Create(Transform parent, FighterDefinition fighter, Color primary,
            Color secondary, Color fabric, Color skin)
        {
            return new FighterMeshRigPresentation(parent, fighter, primary, secondary, fabric, skin);
        }

        public void Present(FighterState state, int tick, FighterVisualState visualState)
        {
            root.localScale = new Vector3(state.Facing, 1f, 1f);
            root.localPosition = new Vector3(0f, 0f, -0.22f);
            NeutralPose();
            float wave = Mathf.Sin((tick % 120) * Mathf.PI / 60f);
            float action = state.Action == CombatAction.None ? 0f :
                Mathf.Clamp01(state.ActionTick / (float)Mathf.Max(1, FighterSimulation.GetAttackSpec(state.Action).DurationTicks));
            float strike = Mathf.Sin(action * Mathf.PI);

            switch (visualState)
            {
                case FighterVisualState.Idle:
                    body.localPosition = new Vector3(0f, wave * 0.025f, 0f);
                    torso.Transform.localRotation = Quaternion.Euler(0f, 0f, wave * 1.8f);
                    head.Transform.localRotation = Quaternion.Euler(0f, 0f, wave * 2.2f);
                    break;
                case FighterVisualState.Advance:
                case FighterVisualState.Retreat:
                    float walk = Mathf.Sin((tick % 24) * Mathf.PI / 12f) * (visualState == FighterVisualState.Retreat ? -1f : 1f);
                    body.localPosition = new Vector3(0f, Mathf.Abs(walk) * 0.06f, 0f);
                    Segment(rearThigh, new Vector2(-0.18f * width, 1.05f * height), -96f + walk * 28f, 0.78f * height);
                    Segment(rearCalf, Endpoint(rearThigh, rearThigh.Transform.localEulerAngles.z, 0.78f * height), 12f - walk * 20f, 0.70f * height);
                    Segment(frontThigh, new Vector2(0.20f * width, 1.05f * height), -78f - walk * 28f, 0.78f * height);
                    Segment(frontCalf, Endpoint(frontThigh, frontThigh.Transform.localEulerAngles.z, 0.78f * height), -18f + walk * 20f, 0.70f * height);
                    frontUpperArm.Transform.localRotation = Quaternion.Euler(0f, 0f, -20f + walk * 18f);
                    rearUpperArm.Transform.localRotation = Quaternion.Euler(0f, 0f, -48f - walk * 18f);
                    break;
                case FighterVisualState.JumpRise:
                    torso.Transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
                    Segment(rearUpperArm, new Vector2(-0.30f * width, 1.90f * height), 45f, 0.66f);
                    Segment(frontUpperArm, new Vector2(0.30f * width, 1.92f * height), 58f, 0.66f);
                    Segment(rearThigh, new Vector2(-0.16f * width, 1.02f * height), -135f, 0.78f);
                    Segment(frontThigh, new Vector2(0.18f * width, 1.02f * height), -42f, 0.78f);
                    break;
                case FighterVisualState.JumpFall:
                    torso.Transform.localRotation = Quaternion.Euler(0f, 0f, -10f);
                    Segment(rearUpperArm, new Vector2(-0.30f * width, 1.90f * height), 145f, 0.66f);
                    Segment(frontUpperArm, new Vector2(0.30f * width, 1.92f * height), 28f, 0.66f);
                    Segment(rearThigh, new Vector2(-0.16f * width, 1.02f * height), -112f, 0.78f);
                    Segment(frontThigh, new Vector2(0.18f * width, 1.02f * height), -64f, 0.78f);
                    break;
                case FighterVisualState.Crouch:
                    body.localPosition = new Vector3(0f, -0.45f, 0f);
                    torso.Transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
                    head.Transform.localPosition += new Vector3(0.22f, -0.22f, 0f);
                    Segment(rearThigh, new Vector2(-0.18f * width, 0.95f * height), -154f, 0.72f);
                    Segment(frontThigh, new Vector2(0.20f * width, 0.95f * height), -28f, 0.72f);
                    break;
                case FighterVisualState.Guard:
                    torso.Transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
                    Segment(rearUpperArm, new Vector2(-0.28f * width, 1.90f * height), 54f, 0.67f);
                    Segment(frontUpperArm, new Vector2(0.32f * width, 1.92f * height), 78f, 0.67f);
                    break;
                case FighterVisualState.LightPunch:
                case FighterVisualState.HeavyPunch:
                    torso.Transform.localRotation = Quaternion.Euler(0f, 0f, -10f + strike * 26f);
                    Segment(frontUpperArm, new Vector2(0.32f * width, 1.91f * height), Mathf.Lerp(-20f, 8f, strike), 0.68f);
                    Segment(frontForearm, Endpoint(frontUpperArm, frontUpperArm.Transform.localEulerAngles.z, 0.68f), Mathf.Lerp(68f, 0f, strike), 0.60f);
                    break;
                case FighterVisualState.LightKick:
                case FighterVisualState.HeavyKick:
                    torso.Transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
                    Segment(frontThigh, new Vector2(0.20f * width, 1.00f * height), Mathf.Lerp(-72f, 28f, strike), 0.80f);
                    Segment(frontCalf, Endpoint(frontThigh, frontThigh.Transform.localEulerAngles.z, 0.80f), Mathf.Lerp(-23f, -18f, strike), 0.68f);
                    break;
                case FighterVisualState.Special:
                    torso.Transform.localRotation = Quaternion.Euler(0f, 0f, -18f + strike * 25f);
                    Segment(frontUpperArm, new Vector2(0.30f * width, 1.92f * height), -5f + strike * 12f, 0.70f);
                    Segment(frontForearm, Endpoint(frontUpperArm, frontUpperArm.Transform.localEulerAngles.z, 0.70f), -6f, 0.64f);
                    break;
                case FighterVisualState.Impact:
                case FighterVisualState.Hit:
                    body.localPosition = new Vector3(-0.13f + strike * 0.20f, 0f, 0f);
                    body.localRotation = Quaternion.Euler(0f, 0f, 12f);
                    head.Transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
                    Segment(frontUpperArm, new Vector2(0.30f * width, 1.90f * height), 122f, 0.68f);
                    break;
                case FighterVisualState.Stun:
                    body.localRotation = Quaternion.Euler(0f, 0f, wave * 7f);
                    head.Transform.localRotation = Quaternion.Euler(0f, 0f, -wave * 12f);
                    break;
                case FighterVisualState.Knockdown:
                    body.localPosition = new Vector3(-0.08f, 0.38f, 0f);
                    body.localRotation = Quaternion.Euler(0f, 0f, 78f);
                    break;
                case FighterVisualState.Finish:
                    body.localPosition = new Vector3(0f, Mathf.Abs(wave) * 0.05f, 0f);
                    Segment(rearUpperArm, new Vector2(-0.30f * width, 1.92f * height), 82f, 0.66f);
                    Segment(frontUpperArm, new Vector2(0.30f * width, 1.92f * height), 98f, 0.66f);
                    break;
            }
            chest.Transform.localPosition = torso.Transform.localPosition + new Vector3(0.16f, 0.12f, -0.32f);
            ApplyColors();
        }

        public void SetFlash(bool value)
        {
            flash = value;
            ApplyColors();
        }

        private void NeutralPose()
        {
            body.localPosition = Vector3.zero;
            body.localRotation = Quaternion.identity;
            torso.Transform.localPosition = new Vector3(0f, 1.58f * height, 0f);
            torso.Transform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            head.Transform.localPosition = new Vector3(0.02f, 2.56f * height, 0f);
            head.Transform.localRotation = Quaternion.identity;
            chest.Transform.localPosition = new Vector3(0.16f, 1.72f * height, -0.32f);
            Segment(rearUpperArm, new Vector2(-0.38f * width, 1.94f * height), -48f, 0.66f);
            Segment(rearForearm, Endpoint(rearUpperArm, rearUpperArm.Transform.localEulerAngles.z, 0.66f), 76f, 0.58f);
            Segment(frontUpperArm, new Vector2(0.42f * width, 1.93f * height), -20f, 0.68f);
            Segment(frontForearm, Endpoint(frontUpperArm, frontUpperArm.Transform.localEulerAngles.z, 0.68f), 68f, 0.60f);
            Segment(rearThigh, new Vector2(-0.24f * width, 1.02f * height), -102f, 0.80f * height);
            Segment(rearCalf, Endpoint(rearThigh, rearThigh.Transform.localEulerAngles.z, 0.80f * height), 15f, 0.68f * height);
            Segment(frontThigh, new Vector2(0.26f * width, 1.02f * height), -72f, 0.80f * height);
            Segment(frontCalf, Endpoint(frontThigh, frontThigh.Transform.localEulerAngles.z, 0.80f * height), -23f, 0.70f * height);
        }

        private void Segment(Part part, Vector2 anchor, float angle, float length)
        {
            Vector2 direction = Direction(angle);
            part.Transform.localPosition = anchor + direction * (length * 0.5f);
            part.Transform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
            part.Transform.localScale = new Vector3(part.Transform.localScale.x, length * 0.5f, part.Transform.localScale.z);
        }

        private static Vector2 Direction(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private static Vector2 Endpoint(Part part, float angle, float length)
        {
            Vector2 direction = Direction(angle);
            Vector3 position = part.Transform.localPosition;
            return new Vector2(position.x + direction.x * length, position.y + direction.y * length);
        }

        private Part Primitive(string name, Transform parent, Color color, float widthValue)
        {
            return Primitive(name, PrimitiveType.Capsule, parent, color, new Vector3(widthValue, 0.33f, widthValue * 0.78f));
        }

        private Part Primitive(string name, PrimitiveType type, Transform parent, Color color, Vector3 scale)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localScale = scale;
            Component collider = item.GetComponent("Collider");
            if (collider != null)
            {
                if (Application.isPlaying) Object.Destroy(collider);
                else Object.DestroyImmediate(collider);
            }
            MeshRenderer renderer = item.GetComponent<MeshRenderer>();
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.05f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.28f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 0.08f);
            }
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Part part = new Part(item.transform, renderer, color);
            parts.Add(part);
            return part;
        }

        private void ApplyColors()
        {
            for (int i = 0; i < parts.Count; i++)
                parts[i].Renderer.sharedMaterial.color = flash ? Color.white : parts[i].BaseColor;
        }
    }
}
