using UnityEngine;

namespace utils {
    public static class ResourceUtils {
        public static Mesh getMesh(this GameObject gameObject, string path) =>
            gameObject.transform.Find(path).GetComponent<MeshFilter>().sharedMesh;
    }
}