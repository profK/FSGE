module ShadersGLSL

open Silk.NET.OpenGL

module Shader =
    let defaultVertexShaderCode = @"
#version 330 core
layout (location = 0) in vec3 aPosition;
void main()
{
    gl_Position = vec4(aPosition, 1.0);
}"
    let defaultFragmentShaderCode = @"
#version 330 core
out vec4 out_color;
void main()
{
    out_color = vec4(1.0, 0.5, 0.2, 1.0);
}"
    
    let tryCompileShader (_gl:GL) code (stype:ShaderType)  =
        let shader = _gl.CreateShader(stype)
        _gl.ShaderSource(shader, code)
        _gl.CompileShader(shader);
        let mutable fstatus = 0
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, &fstatus)
        match enum<GLEnum> fstatus with
        | GLEnum.True -> (Some shader,"OK")
        | _ -> (None,_gl.GetShaderInfoLog(shader))
        
    let compileShader (_gl:GL) code stype =
        let result = tryCompileShader _gl code stype
        match fst result with
        | Some shader -> shader
        | None -> failwith $"Shader failed to compile: {snd result}"
                
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
    let linkShaders (_gl:GL)  (shaders) =
        let result = tryLinkShaders (_gl:GL) shaders
        match fst result with
        | Some program  -> program
        | None -> failwith $"Shader program failed to link: {snd result}"
                  
    let getDefaultShaderProgram (_gl:GL) =
       let result =
            [compileShader _gl defaultVertexShaderCode ShaderType.VertexShader;
            compileShader _gl defaultFragmentShaderCode ShaderType.FragmentShader]
            |> linkShaders _gl
       _gl.EnableVertexAttribArray(0u);
       _gl.VertexAttribPointer(0u, 3, GLEnum.Float, false,uint32 (3 * sizeof<float>), nativeint 0)
       result
    