//
// Program.cs
//
// Copyright (C) 2018 OpenTK
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ObjectTK.Exceptions;
using ObjectTK.Shaders.Variables;
using OpenTK.Graphics.OpenGL4;

namespace ObjectTK.Shaders
{
    /// <summary>
    /// Represents a program object.
    /// </summary>
    public class Program
        : GLObject
    {
        private static readonly Logging.IObjectTKLogger Logger = Logging.LogFactory.GetLogger(typeof(Program));

        /// <summary>
        /// The name of this shader program.
        /// </summary>
        public string Name { get { return GetType().Name; } }

        public List<ProgramVariable> Variables { get => _variables; set => _variables = value; }

        private List<ProgramVariable> _variables;
        private List<ProgramVariable> _old_variables;

        /// <summary>
        /// Initializes a new program object.
        /// </summary>
        protected Program()
            : base(GL.CreateProgram())
        {
            Logger?.InfoFormat("Creating shader program: {0}", Name);
            InitializeShaderVariables();
        }

        protected override void Dispose(bool manual)
        {
            if (!manual) return;
            GL.DeleteProgram(Handle);
        }

        private void InitializeShaderVariables()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            _variables = new List<ProgramVariable>();
            foreach (var property in GetType().GetProperties(flags).Where(_ => typeof(ProgramVariable).IsAssignableFrom(_.PropertyType)))
            {
                var instance = (ProgramVariable)Activator.CreateInstance(property.PropertyType, true);
                instance.Initialize(this, property);
                property.SetValue(this, instance, null);
                _variables.Add(instance);
            }
        }

        private void ReinitializeShaderVariables()
        {
            foreach(var _shader_var in _variables)
            {
                PropertyInfo nfo = this.GetType().GetProperty(_shader_var.Name);
                _shader_var.Initialize(this, nfo);
            }
            return;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            _variables = new List<ProgramVariable>();

            foreach (var property in GetType().GetProperties(flags).Where(_ => typeof(ProgramVariable).IsAssignableFrom(_.PropertyType)))
            {
                var instance = (ProgramVariable)Activator.CreateInstance(property.PropertyType, true);
                instance.Initialize(this, property);
                property.SetValue(this, instance, null);
                _variables.Add(instance);
            }

            
        }

        /// <summary>
        /// Activate the program.
        /// </summary>
        public void Use()
        {
            GL.UseProgram(Handle);
        }

        /// <summary>
        /// retrieves a new shader progamm handle from gl
        /// Should only be called by the programm factory
        /// RestoreUniforms should get called after succesfull recompilation
        /// </summary>
        public void Recreate()
        {
            Logger?.InfoFormat("Recreating shader program {0}", Name);
            _old_variables = _variables;
            GL.DeleteProgram(Handle);
            _handle = GL.CreateProgram();
            
            InitializeShaderVariables();
            
            
        }

        /// <summary>
        /// saves a backup of the current uniform variables
        /// </summary>
        public void BackupUniforms()
        {
            _old_variables = _variables;
        }

        /// <summary>
        /// tries to restore old uniforms previously saved with BackupUniforms or Recreate
        /// </summary>
        public void RestoreUniforms()
        {
            if (_old_variables == null)
                return;
            this.Use();
            foreach (var oldVar in _old_variables.Where(x => x.GetType().GetInterface("IUniform") != null))
            {
                var old_uniform = oldVar as IUniform;
                var new_uniform = GetType().GetProperty(oldVar.Name).GetValue(this) as IUniform;
                new_uniform.GetType().GetProperty("Value").SetValue(new_uniform, oldVar.GetType().GetProperty("Value").GetValue(old_uniform));// TrySetValue(old_uniform.GetValue());

            }

        }

        /// <summary>
        /// Attach shader object.
        /// </summary>
        /// <param name="shader">Specifies the shader object to attach.</param>
        public void Attach(Shader shader)
        {
            GL.AttachShader(Handle, shader.Handle);
        }

        /// <summary>
        /// Detach shader object.
        /// </summary>
        /// <param name="shader">Specifies the shader object to detach.</param>
        public void Detach(Shader shader)
        {
            GL.DetachShader(Handle, shader.Handle);
        }

        /// <summary>
        /// Link the program.
        /// </summary>
        public virtual void Link()
        {
            Logger?.DebugFormat("Linking program: {0}", Name);
            GL.LinkProgram(Handle);
            CheckLinkStatus();
            // call OnLink() on all ShaderVariables
            _variables.ForEach(_ => _.OnLink());
        }

        /// <summary>
        /// Assert that no link error occured.
        /// </summary>
        private void CheckLinkStatus()
        {
            // check link status
            int linkStatus;
            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out linkStatus);
            Logger?.DebugFormat("Link status: {0}", linkStatus);
            // check program info log
            var info = GL.GetProgramInfoLog(Handle);
            if (!string.IsNullOrEmpty(info)) Logger?.InfoFormat("Link log:\n{0}", info);
            // log message and throw exception on link error
            if (linkStatus == 1) return;
            var msg = string.Format("Error linking program: {0}", Name);
            Logger?.Error(msg);
            throw new ProgramLinkException(msg, info);
        }

        /// <summary>
        /// Throws an <see cref="ObjectNotBoundException"/> if this program is not the currently active one.
        /// </summary>
        public void AssertActive()
        {
#if DEBUG
            int activeHandle;
            GL.GetInteger(GetPName.CurrentProgram, out activeHandle);
            if (activeHandle != Handle) throw new ObjectNotBoundException("Program object is not currently active.");
#endif
        }
    }
}