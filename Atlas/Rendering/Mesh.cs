using Veldrid;

namespace SolidCode.Atlas.Rendering;

public class Mesh<T> where T : struct
{
    public ushort[] Indices;
    public VertexLayoutDescription VertexLayout;
    public T[] Vertices;

    public Mesh(T[] vertices, ushort[] indices, VertexLayoutDescription vertexLayout)
    {
        Vertices = vertices;
        Indices = indices;
        VertexLayout = vertexLayout;
    }

    public Mesh(Mesh<T> mesh)
    {
        Vertices = (T[])mesh.Vertices.Clone();
        Indices = (ushort[])mesh.Indices.Clone();
        VertexLayout = mesh.VertexLayout;
    }

    public void AddVertices(T[] vertices)
    {
        var a = (T[])Vertices.Clone();
        var b = (T[])vertices.Clone();
        var z = new T[a.Length + b.Length];
        a.CopyTo(z, 0);
        b.CopyTo(z, a.Length);
        Vertices = z;
    }

    public void AddIndices(ushort[] indices)
    {
        var a = (ushort[])Indices.Clone();
        var b = (ushort[])indices.Clone();
        var z = new ushort[a.Length + b.Length];
        a.CopyTo(z, 0);
        b.CopyTo(z, a.Length);
        Indices = z;
    }

    public void ClearIndices()
    {
        Indices = new ushort[0];
    }

    public void ClearVertices()
    {
        Vertices = new T[0];
    }
}