using Godot;
using Godot.Collections;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Settings;

namespace Simulation;

#region Structs

// This is all helplessly overengineered but I like it

// https://github.com/godotengine/godot-docs-user-notes/discussions/17#discussioncomment-9784442
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agent
{
    public Vector2 position;
    public float direction;
    public int species;
    public Vector4 speciesMask;
}

// explicit layout is necessary for the color to be at the right offset
[StructLayout(LayoutKind.Explicit)]
public struct SpeciesSettings
{
    [FieldOffset(0)] public float moveSpeed;
    [FieldOffset(4)] public float turnSpeed;
    [FieldOffset(8)] public float sensorAngle;
    [FieldOffset(12)] public float sensorDistance;
    [FieldOffset(16)] public float sensorSize;
    [FieldOffset(32)] public Color color;
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
public struct PushConstant
{
    // public int texSizeX;
    // public int texSizeY;
    public Vector2I texSize;
    // public int numAgents;
    // public int numSpecies;
    public float trailWeight;
    public float deltaTime;
    public uint ticks;
    public float decaySpeed;
    public float diffuseSpeed;
    public int agentBehavior;
}


#endregion

public partial class SlimeSimulation : Node2D
{


    // debug stuff
    // debug buffer is an array of 10 floats
    // write to it in the shader to get some kind of debug output
    int debugCount = 10;
    Rid debugBuffer;
    Rid debugUniformSet;



    RenderingDevice RD;

    // Agent stuff
    Rid AgentShader;
    Rid AgentBuffer;
    Rid AgentUniformSet;
    Rid AgentPipeline;

    Agent[] Agents;


    TextureRect TextureRect;
    TextureRect Background;


    Rid Trailmap; // the texture itself
    Rid TrailmapUniformSet;


    Rid DiffuseShader;
    Rid DiffusePipeline;
    Rid DiffuseMapUniformSet;


    Rid ColormapShader;
    Rid ColormapPipeline;
    Rid Colormap;
    Rid ColormapUniformSet;

    Rid TestingShader;
    Rid TestingPipeline;



    SpeciesSettings[] SpeciesSettings;

    // = new SpeciesSettings[]
    // {
    // 	new()
    // 	{
    // 		moveSpeed = 40.0f,
    // 		turnSpeed = 1,
    // 		sensorAngle = 0.79f,
    // 		sensorDistance = 15f,
    // 		sensorSize = 4f,
    // 		color = new(1,0,1,1)
    // 	},
    // 	new()
    // 	{
    // 		moveSpeed = 40.0f,
    // 		turnSpeed = 1,
    // 		sensorAngle = 0.79f,
    // 		sensorDistance = 15f,
    // 		sensorSize = 4f,
    // 		color = new(0,1,1,1)
    // 	},
    // 	new()
    // 	{
    // 		moveSpeed = 40.0f,
    // 		turnSpeed = 1,
    // 		sensorAngle = 0.79f,
    // 		sensorDistance = 15f,
    // 		sensorSize = 4f,
    // 		color = new(1,1,0,1)
    // 	},
    // 	new()
    // 	{
    // 		moveSpeed = 40.0f,
    // 		turnSpeed = 1,
    // 		sensorAngle = 0.79f,
    // 		sensorDistance = 15f,
    // 		sensorSize = 4f,
    // 		color = new(1,0,0,1)
    // 	}
    // };
    Rid SpeciesSettingsBuffer;
    Rid SpeciesSettingsUniformSet;


    #region Exports

    // [ExportGroup("General Settings")]

    // [Export]
    Vector2I TextureSize = new(1920, 1080);
    // [Export]
    int NumAgents = 65536;
    // [Export]
    float DiffuseSpeed = 10.0f;
    // [Export]
    float DimSpeed = 0.12f;
    // [Export]
    float TrailWeight = 5f;

    int AgentBehavior = 0;

    //? For some reason changes in editor had no effet when it was already initialized
    [Export] public Settings.Settings Settings;

    #endregion



    // change this once you need a real method
    // for now, this will suffice for testing
    public void CreateAgents(int numAgents)
    {
        // Agents = new Agent[numAgents];
        // for (int i = 0; i < numAgents; i++)
        // {
        // 	Agents[i] = new Agent
        // 	{
        // 		position = new Vector2(i, i),
        // 		direction = i
        // 	};
        // }
        Agents = new Agent[numAgents];

        // for (int i = 0; i < numAgents; i++)
        // {
        // 	// in the middle, facing outwards in cicle
        // 	Agents[i] = new Agent
        // 	{
        // 		position = new Vector2(TextureSize.X / 2, TextureSize.Y / 2),
        // 		// position = new Vector2(GD.Randi() % (TextureSize.X / 2), GD.Randi() % (TextureSize.Y / 2)),
        // 		direction = i * Mathf.Pi * 2 / numAgents,

        // 		species = 0,
        // 		speciesMask = new Vector4(1, 0, 0, 0)
        // 		// species = i % 2 == 0 ? 0 : 1,
        // 		// speciesMask = i % 2 == 0 ? new Vector4(1, 0, 0, 0) : new Vector4(0, 1, 0, 1)
        // 	};
        // }

        // in circle, facing center
        int radius = Math.Min(TextureSize.X, TextureSize.Y) / 2;
        for (int i = 0; i < numAgents; i++)
        {
            float theta = GD.Randf() * Mathf.Pi * 2;
            Vector2 vec = new(Mathf.Cos(theta), Mathf.Sin(theta));
            vec *= Mathf.Sqrt(GD.Randf()) * radius;
            float dir = Mathf.Atan2(-vec.Y, -vec.X);
            vec += new Vector2(TextureSize.X / 2, TextureSize.Y / 2);

            // vec = new Vector2(TextureSize.X / 2, TextureSize.Y / 2);
            // dir = 2 * Mathf.Pi / numAgents * i;

            // vec = new Vector2(GD.Randi() % TextureSize.X, GD.Randi() % TextureSize.Y);
            // dir = GD.Randf() * Mathf.Pi * 2;

            int species = i % SpeciesSettings.Length;
            Vector4 mask = new(
                species == 0 ? 1 : 0,
                species == 1 ? 1 : 0,
                species == 2 ? 1 : 0,
                species == 3 ? 1 : 0
            );

            Agents[i] = new Agent
            {
                position = vec,
                direction = dir,
                // species = i % 2 == 0 ? 0 : 1,
                // speciesMask = i % 2 == 0 ? new Vector4(1, 0, 0, 0) : new Vector4(0, 1, 0, 0)
                species = species,
                speciesMask = mask
            };
        }
        // // used to see where the angle of 0 points to
        // if (numAgents == 1)
        // {
        // 	Agents = new Agent[]
        // 	{
        // 		new()
        // 		{
        // 			position = new Vector2(TextureSize.X / 2, TextureSize.Y / 2),
        // 			direction = 0,
        // 			species = 0,
        // 			speciesMask = new Vector4(1, 0, 0, 0)
        // 		}
        // 	};
        // }
    }



    Rid CreateImageUniformSet(Rid texture, Rid shader, uint set)
    {
        var textureUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.Image,
            Binding = 0,
        };
        textureUniform.AddId(texture);
        return RD.UniformSetCreate(new Array<RDUniform> { textureUniform }, shader, set);

    }

    public void InitShaders(int numAgents, Vector2I texSize)
    {
        // get the rendering device
        RD = RenderingServer.GetRenderingDevice();

        //** SHADERS **//

        // Agents
        var agentShaderFile = GD.Load<RDShaderFile>("res://shaders/update_agents.glsl");
        var agentShaderBytecode = agentShaderFile.GetSpirV();
        AgentShader = RD.ShaderCreateFromSpirV(agentShaderBytecode);
        AgentPipeline = RD.ComputePipelineCreate(AgentShader);

        // Diffuse
        var diffuseShaderFile = GD.Load<RDShaderFile>("res://shaders/diffuse_texture.glsl");
        var diffuseShaderBytecode = diffuseShaderFile.GetSpirV();
        DiffuseShader = RD.ShaderCreateFromSpirV(diffuseShaderBytecode);
        DiffusePipeline = RD.ComputePipelineCreate(DiffuseShader);

        // Colormap
        var colormapShaderFile = GD.Load<RDShaderFile>("res://shaders/update_colormap.glsl");
        var colormapShaderBytecode = colormapShaderFile.GetSpirV();
        ColormapShader = RD.ShaderCreateFromSpirV(colormapShaderBytecode);
        ColormapPipeline = RD.ComputePipelineCreate(ColormapShader);

        //? Testing
        var testingShaderFile = GD.Load<RDShaderFile>("res://shaders/testing.glsl");
        var testingShaderBytecode = testingShaderFile.GetSpirV();
        TestingShader = RD.ShaderCreateFromSpirV(testingShaderBytecode);
        TestingPipeline = RD.ComputePipelineCreate(TestingShader);

        //** BUFFERS**//

        // create agents
        CreateAgents(numAgents);

        // create buffer for agents
        var agentSpan = MemoryMarshal.CreateSpan(ref Agents[0], Agents.Length);
        var agentBytes = MemoryMarshal.AsBytes(agentSpan).ToArray();

        AgentBuffer = RD.StorageBufferCreate((uint)agentBytes.Length, agentBytes);

        // create uniform for agents
        var agentUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0,
        };
        agentUniform.AddId(AgentBuffer);
        AgentUniformSet = RD.UniformSetCreate(new Array<RDUniform> { agentUniform }, AgentShader, 0);

        // settings
        var speciesSettingsSpan = MemoryMarshal.CreateSpan(ref SpeciesSettings[0], SpeciesSettings.Length);
        var speciesSettingsBytes = MemoryMarshal.AsBytes(speciesSettingsSpan).ToArray();
        SpeciesSettingsBuffer = RD.StorageBufferCreate((uint)speciesSettingsBytes.Length, speciesSettingsBytes);

        var speciesSettingsUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0,
        };
        speciesSettingsUniform.AddId(SpeciesSettingsBuffer);
        SpeciesSettingsUniformSet = RD.UniformSetCreate(new Array<RDUniform> { speciesSettingsUniform }, AgentShader, 4);

        // debug
        var debugBytes = new byte[debugCount * sizeof(float)];
        debugBuffer = RD.StorageBufferCreate((uint)debugBytes.Length, debugBytes);

        var debugUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0,
        };
        debugUniform.AddId(debugBuffer);
        debugUniformSet = RD.UniformSetCreate(new Array<RDUniform> { debugUniform }, AgentShader, 5);

        //** TEXTURE **//
        // create texture format
        var textureFormat = new RDTextureFormat
        {
            Format = RenderingDevice.DataFormat.R8G8B8A8Unorm,
            TextureType = RenderingDevice.TextureType.Type2D,
            Width = (uint)texSize.X,
            Height = (uint)texSize.Y,
            Depth = 1,
            ArrayLayers = 1,
            Mipmaps = 1,
            UsageBits =
                RenderingDevice.TextureUsageBits.SamplingBit |
                RenderingDevice.TextureUsageBits.StorageBit |
                RenderingDevice.TextureUsageBits.CanUpdateBit |
                RenderingDevice.TextureUsageBits.CanCopyToBit
        };

        // create texture
        Trailmap = RD.TextureCreate(textureFormat, new RDTextureView());
        //* FILL IT WITH TRANSPARENT
        //! fill it with black
        //* IF YOU FILL WITH BLACK, ALPHA WILL BE 1
        //* EVERYTHING WILL BE OF THE COLOR OF SPECIES 3 AT BEGIN, UNTIL DECAY TAKES CARE OF IT
        //! RD.TextureClear(Trailmap, new Color(0, 0, 0, 1), 0, 1, 0, 1);
        RD.TextureClear(Trailmap, new Color(0, 0, 0, 0), 0, 1, 0, 1);

        // create colormap
        Colormap = RD.TextureCreate(textureFormat, new RDTextureView());
        // fill it with black
        //* SAME APPLIES HERE
        RD.TextureClear(Colormap, new Color(0, 0, 0, 0), 0, 1, 0, 1);
        //* BLACK IS UNNECESSARY, BECAUSE THE BACKGROUND EXISTS

        //** UNIFORM SETS **//

        TrailmapUniformSet = CreateImageUniformSet(Trailmap, AgentShader, 1);

        DiffuseMapUniformSet = CreateImageUniformSet(Trailmap, DiffuseShader, 2);

        ColormapUniformSet = CreateImageUniformSet(Colormap, ColormapShader, 3);

        // ComputeList = RD.ComputeListBegin();

    }

    public void RenderProcess(float delta)
    {
        // create push constant
        PushConstant push = new()
        {
            // texSizeX = TextureSize.X,
            // texSizeY = TextureSize.Y,
            texSize = TextureSize,
            // numAgents = NumAgents,
            // numSpecies = SpeciesSettings.Length,
            trailWeight = TrailWeight,
            deltaTime = delta,
            // epochTime = (float)Time.GetUnixTimeFromSystem(),
            ticks = (uint)Time.GetTicksUsec(), // maybe use msec?
            decaySpeed = DimSpeed,
            diffuseSpeed = DiffuseSpeed,
            agentBehavior = AgentBehavior
        };


        var span = MemoryMarshal.CreateSpan(ref push, 1);
        var pushBytes = MemoryMarshal.AsBytes(span).ToArray();


        var computeList = RD.ComputeListBegin();


        BindUniformSets(computeList);
        RunShader(computeList, DiffusePipeline, pushBytes, (uint)((TextureSize.X - 1) / 8 + 1), (uint)((TextureSize.Y - 1) / 8 + 1), 1);
        RD.ComputeListAddBarrier(computeList);

        BindUniformSets(computeList);
        RunShader(computeList, ColormapPipeline, pushBytes, (uint)((TextureSize.X - 1) / 8 + 1), (uint)((TextureSize.Y - 1) / 8 + 1), 1);
        RD.ComputeListAddBarrier(computeList);

        BindUniformSets(computeList);
        RunShader(computeList, AgentPipeline, pushBytes, (uint)(NumAgents - 1) / 16 + 1, 1, 1);
        RD.ComputeListAddBarrier(computeList);


        // BindUniformSets(computeList);
        // RunShader(computeList, TestingPipeline, pushBytes, (uint)(NumAgents - 1) / 16 + 1, 1, 1);
        // RD.ComputeListAddBarrier(computeList);


        RD.ComputeListEnd();


        // debug results
        var debugBytes = RD.BufferGetData(debugBuffer);
        var debugSpan = MemoryMarshal.Cast<byte, uint>(debugBytes);
        var debugString = string.Join(", ", debugSpan.ToArray());
        // GD.Print(debugString);
    }

    public void BindUniformSets(long computeList)
    {
        RD.ComputeListBindUniformSet(computeList, AgentUniformSet, 0);
        RD.ComputeListBindUniformSet(computeList, TrailmapUniformSet, 1);
        RD.ComputeListBindUniformSet(computeList, DiffuseMapUniformSet, 2);
        RD.ComputeListBindUniformSet(computeList, ColormapUniformSet, 3);
        RD.ComputeListBindUniformSet(computeList, SpeciesSettingsUniformSet, 4);
        RD.ComputeListBindUniformSet(computeList, debugUniformSet, 5);
    }

    // this assumes the uniforms have already been bound to the compute list
    public void RunShader(long computeList, Rid pipeline, byte[] pushConstant, uint xGroups, uint yGroups, uint zGroups)
    {
        RD.ComputeListBindComputePipeline(computeList, pipeline);
        RD.ComputeListSetPushConstant(computeList, pushConstant, (uint)pushConstant.Length);
        RD.ComputeListDispatch(computeList, xGroups, yGroups, zGroups);
    }

    bool first = true;

    // Called when the node enters the scene tree for the first time.
    public override async void _Ready()
    {
        // Load the settings
        //? Kinda stupid, but should suffice for now
        TextureSize = Settings.MapSize;
        NumAgents = Settings.NumAgents;
        DiffuseSpeed = Settings.DiffuseSpeed;
        DimSpeed = Settings.DecaySpeed;
        TrailWeight = Settings.TrailWeight;
        AgentBehavior = Settings.AgentBehavior;

        SpeciesSettings = new SpeciesSettings[Settings.Species.Length];
        for (int i = 0; i < Settings.Species.Length; i++)
        {
            SpeciesSettings[i] = new SpeciesSettings
            {
                moveSpeed = Settings.Species[i].MoveSpeed,
                turnSpeed = Settings.Species[i].TurnSpeed,
                sensorAngle = Settings.Species[i].SensorAngle,
                sensorDistance = Settings.Species[i].SensorDistance,
                sensorSize = Settings.Species[i].SensorSize,
                color = Settings.Species[i].Color
            };
        }


        GetTree().Root.SizeChanged += () =>
        {
            // TextureSize = (Vector2I)GetViewportRect().Size;
            TextureRect.Size = GetViewportRect().Size;

            Background.Size = GetViewportRect().Size;
        };

        TextureRect = GetNode<TextureRect>("TextureRect");
        // TextureRect.Size = TextureSize;
        TextureRect.Size = GetViewportRect().Size;
        TextureRect.Texture = new Texture2Drd();

        Vector2 viewportSize = GetViewportRect().Size;
        Background = GetNode<TextureRect>("Background");
        Image bgImage = Image.CreateEmpty((int)viewportSize.X, (int)viewportSize.Y, false, Image.Format.Rgba8);
        bgImage.Fill(new(0, 0, 0, 1));
        ImageTexture bgTexture = ImageTexture.CreateFromImage(bgImage);
        Background.Texture = bgTexture;
        Background.Size = viewportSize;


        RenderingServer.CallOnRenderThread(Callable.From(() => InitShaders(NumAgents, TextureSize)));

        // set the Rid of the texture created in `InitShader` to the texture of the TextureRect
        // (TextureRect.Texture as Texture2Drd).TextureRdRid = Trailmap;
        (TextureRect.Texture as Texture2Drd).TextureRdRid = Colormap;


        if (AgentBehavior == 3)
        {
            GetViewport().GuiEmbedSubwindows = false;

            PackedScene tripleViewScene = GD.Load<PackedScene>("res://scenes/TripleView.tscn");

            TripleView tripleView = tripleViewScene.Instantiate<TripleView>();
            AddChild(tripleView);
            tripleView.Visible = true;
            tripleView.Size = (Vector2I)viewportSize;
            tripleView.Title = "Triple View";
            tripleView.Position = new Vector2I(100, 100);
            tripleView.GetReady();
            tripleView.SetTexture(Colormap);
        }

        await ToSignal(GetTree().CreateTimer(2), SceneTreeTimer.SignalName.Timeout);
        first = false;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (first) return;
        RenderingServer.CallOnRenderThread(Callable.From(() => RenderProcess((float)delta)));
        // GD.Print(Engine.GetFramesPerSecond());
    }
}
