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

        public void AddVertices(T[] vertices) {
            var z = new T[Vertices.Length + vertices.Length];
            Vertices.CopyTo(z, 0);
            vertices.CopyTo(z, Vertices.Length);
            Vertices = z;
        }

        public void AddIndicies(ushort[] indicies) {
            var z = new ushort[Indicies.Length + indicies.Length];
            Indicies.CopyTo(z, 0);
            indicies.CopyTo(z, Indicies.Length);
            Indicies = z;
        }
    }
}