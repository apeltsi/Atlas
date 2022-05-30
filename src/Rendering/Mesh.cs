using Veldrid;

namespace SolidCode.Caerus.Rendering
{
    public class Mesh<T> where T : struct
    {
        public T[] Vertices;
        public ushort[] Indicies;
        public VertexLayoutDescription VertexLayout;

        public Mesh(T[] vertecies, ushort[] indicies, VertexLayoutDescription vertexLayout)
        {
            this.Vertices = vertecies;
            this.Indicies = indicies;
            this.VertexLayout = vertexLayout;
        }
    }
}