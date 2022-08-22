using Veldrid;

namespace SolidCode.Caerus.Rendering
{
    public class Mesh<T> where T : struct
    {
        public T[] Vertices;
        public ushort[] Indicies;
        public VertexLayoutDescription VertexLayout;

        public Mesh(T[] vertices, ushort[] indicies, VertexLayoutDescription vertexLayout)
        {
            this.Vertices = vertices;
            this.Indicies = indicies;
            this.VertexLayout = vertexLayout;
        }

        public Mesh(Mesh<T> mesh) {
            
            this.Vertices = (T[])mesh.Vertices.Clone();
            this.Indicies = (ushort[])mesh.Indicies.Clone();
            this.VertexLayout = mesh.VertexLayout;
        }

        public void AddVertices(T[] vertices) {
            var a = (T[])Vertices.Clone();
            var b = (T[])vertices.Clone();
            var z = new T[a.Length + b.Length];
            a.CopyTo(z, 0);
            b.CopyTo(z, a.Length);
            Vertices = z;
        }

        public void AddIndicies(ushort[] indicies) {
            var a = (ushort[])Indicies.Clone();
            var b = (ushort[])indicies.Clone();
            var z = new ushort[a.Length + b.Length];
            a.CopyTo(z, 0);
            b.CopyTo(z, a.Length);
            Indicies = z;
        }

        public void ClearIndicies() {
            Indicies = new ushort[0];
        }
        public void ClearVertices() {
            Vertices = new T[0];
        }
    }
}