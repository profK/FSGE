
// This module contains the shader code for the
// OpenGL implementation of the graphics library
// It is dependant on the Silk.NET OpenGL library and written
// in the GLSL language.
// It contains both the code for the default vertex and fragment shaders
// as well as functions to compile and link shaders
module ShadersGLSL

open Silk.NET.OpenGL

// This module contains the shader code for the
// OpenGL implementation of the graphics library
module Shader =
    // This bind contians the default vertex shader code
    // as a string
    let defaultVertexShaderCode = @"
#version 330 core

layout (location = 0) in vec3 aPosition;
// Add a new input attribute for the texture coordinates
layout (location = 1) in vec2 aTextureCoord;
uniform mat4 xformMatrix;


// Add an output variable to pass the texture coordinate to the fragment shader
// This variable stores the data that we want to be received by the fragment
out vec2 frag_texCoords;

void main()
{
    gl_Position = xformMatrix * vec4(aPosition, 1.0);
    // Assigin the texture coordinates without any modification to be recived in the fragment
    frag_texCoords = aTextureCoord;
}"

    // This bind contians the default fragment shader code
    // as a string
    let defaultFragmentShaderCode = @"
#version 330 core

// Receive the input from the vertex shader in an attribute
in vec2 frag_texCoords;
uniform sampler2D uTexture;
uniform vec4 tint;

out vec4 out_color;

void main()
{
    // This will allow us to see the texture coordinates in action!
    //out_color = vec4(frag_texCoords.x, frag_texCoords.y, 0.0, 1.0);
    out_color = texture(uTexture, frag_texCoords)*tint;
}"
    
    // This function attempts to compile a shader
    // It takes a string of code, a shader type and a GL object
    // It returns an optional shader object and a string message
    // If the shader compiles successfully, the shader object is returned
    // If the shader fails to compile, None is returned and the message
    // contains the error message
    let tryCompileShader (_gl:GL) code (stype:ShaderType)  =
        let shader = _gl.CreateShader(stype)
        _gl.ShaderSource(shader, code)
        _gl.CompileShader(shader);
        let mutable fstatus = 0
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, &fstatus)
        match enum<GLEnum> fstatus with
        | GLEnum.True -> (Some shader,"OK")
        | _ -> (None,_gl.GetShaderInfoLog(shader))
        
     // This function compiles a shader
     // it wraps tryCompileShader and throws an exception if the shader
     // fails to compile
    let compileShader (_gl:GL) code stype =
        let result = tryCompileShader _gl code stype
        match fst result with
        | Some shader -> shader
        | None -> failwith $"Shader failed to compile: {snd result}"
    // This function attempts to link a shader program
    // It takes a sequence of shader objects and a GL object
    // It returns an optional program object and a string message
    // If the program links successfully, the program object is returned
    // If the program fails to link, None is returned and the message
    // contains the error message 
    let tryLinkShaders (_gl:GL) (shaders:uint32 seq) =
        let program =_gl.CreateProgram()
        shaders |> Seq.iter (fun s -> _gl.AttachShader(program, s))
        _gl.LinkProgram(program)
        shaders |> Seq.iter (fun s -> _gl.DetachShader(program, s))
        let mutable lstatus = 0
        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, &lstatus)
        match enum<GLEnum> lstatus with
        | GLEnum.True -> (Some program,"OK")
        | _ -> (None,_gl.GetProgramInfoLog(program))
        
    // This function links a shader program
    // it wraps tryLinkShaders and throws an exception if the program
    // fails to link
    let linkShaders (_gl:GL)  (shaders) =
        let result = tryLinkShaders (_gl:GL) shaders
        match fst result with
        | Some program  -> program
        | None -> failwith $"Shader program failed to link: {snd result}"
          
    // This function returns the default shader program
    // It takes a GL object as an argument
    // It returns a program object that is the default shader program
    // for the graphics library
    // It compiles the default vertex and fragment shaders every
    // time it is called so the result should be cached by the caller
    let getDefaultShaderProgram (_gl:GL) =
       let result =
            [compileShader _gl defaultVertexShaderCode ShaderType.VertexShader;
            compileShader _gl defaultFragmentShaderCode ShaderType.FragmentShader]
            |> linkShaders _gl
       _gl.EnableVertexAttribArray(0u);
       _gl.VertexAttribPointer(0u, 3, GLEnum.Float, false,uint32 (3 * sizeof<float>), nativeint 0)
       let location = _gl.GetUniformLocation(result, "uTexture");
       _gl.Uniform1(location, 0);
       result
    