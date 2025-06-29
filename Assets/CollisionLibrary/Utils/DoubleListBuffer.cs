using System;
using Unity.Collections;

internal struct DoubleListBuffer<T>: IDisposable where T: unmanaged
{
    private bool useWriteBuffer;
    private NativeList<T> listA;
    private NativeList<T> listB;

    internal DoubleListBuffer(int capacity)
    {
        useWriteBuffer = true;
        listA = new NativeList<T>(capacity, Allocator.Persistent);
        listB = new NativeList<T>(capacity, Allocator.Persistent);
    }
    public NativeList<T> Writer => useWriteBuffer ? listA : listB;

    public NativeList<T> Reader => useWriteBuffer ? listB : listA;


    internal void SwapBuffer()
    {
        useWriteBuffer = !useWriteBuffer;
        Writer.Clear();
    }

    public void Dispose()
    {
        if (listA.IsCreated) listA.Dispose();
        if (listB.IsCreated) listB.Dispose();
    }
}

internal struct DoubleBufferedMultiHashMap<TKey, TValue>
    where TKey : unmanaged, IEquatable<TKey>
    where TValue : unmanaged
{
    private NativeParallelMultiHashMap<TKey, TValue> bufferA;
    private NativeParallelMultiHashMap<TKey, TValue> bufferB;
    private bool useBufferAForWriting;

    public DoubleBufferedMultiHashMap(int initialCapacity, Allocator allocator)
    {
        bufferA = new NativeParallelMultiHashMap<TKey, TValue>(initialCapacity, allocator);
        bufferB = new NativeParallelMultiHashMap<TKey, TValue>(initialCapacity, allocator);
        useBufferAForWriting = true;
    }

    public NativeParallelMultiHashMap<TKey, TValue> GetWriteBuffer()
    {
        return useBufferAForWriting ? bufferA : bufferB;
    }

    public NativeParallelMultiHashMap<TKey, TValue> GetReadBuffer()
    {
        return useBufferAForWriting ? bufferB : bufferA;
    }

    public NativeParallelMultiHashMap<TKey, TValue> SwapAndGetWriteBuffer()
    {
        SwapBuffers();
        return GetWriteBuffer();
    }

    private void SwapBuffers()
    {
        useBufferAForWriting = !useBufferAForWriting;

        var writeBuffer = GetWriteBuffer();
        writeBuffer.Clear();
    }

    public void Dispose()
    {
        if (bufferA.IsCreated) bufferA.Dispose();
        if (bufferB.IsCreated) bufferB.Dispose();
    }
}