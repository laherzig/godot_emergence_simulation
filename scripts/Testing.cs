using Godot;
using System;
using System.Runtime.InteropServices;

public partial class Testing : Node
{

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Test
	{
		public int a;
		public int b;
		public int c;
		public int d;
		public Vector4I vec;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Test test = new()
		{
			a = 1,
			b = 2,
			c = 3,
			d = 4,
			vec = new Vector4I(1, 2, 3, 4)
		};

		MemoryMarshal.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref test, 1)), ref test);

		var span = MemoryMarshal.CreateSpan(ref test, 1);

		GD.Print("Single Test:");
		GD.Print("a: " + span[0].a);
		GD.Print("b: " + span[0].b);
		GD.Print("c: " + span[0].c);
		GD.Print("d: " + span[0].d);
		GD.Print("vec: " + span[0].vec);

		byte[] bytes = MemoryMarshal.AsBytes(span).ToArray();
		GD.Print("bytes: " + BitConverter.ToString(bytes));


		GD.Print("Array Test:");
		Test[] tests = new Test[2]
		{
			new() {
				a = 1,
				b = 2,
				c = 3,
				d = 4,
				vec = new Vector4I(5, 6, 7, 8)
			},
			new() {
				a = 9,
				b = 10,
				c = 11,
				d = 12,
				vec = new Vector4I(13, 14, 15, 16)
			}
		};

		// ok, this works
		var testsSpan = MemoryMarshal.CreateSpan(ref tests[0], tests.Length);
		byte[] testsBytes = MemoryMarshal.AsBytes(testsSpan).ToArray();
		GD.Print(testsSpan.Length);
		GD.Print(testsBytes.Length);
		GD.Print("testsBytes: " + BitConverter.ToString(testsBytes));

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
