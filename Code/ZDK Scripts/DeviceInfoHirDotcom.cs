using Zynga.Core.Platform;

 #if UNITY_WEBGL
 /// <summary>
 /// HIR extension implementation of DeviceInfoWrapper.
 /// 
 /// Instantiate as replacement implementation instance of DeviceInfo on DotCom WebPlatform, in order to override
 /// ClientId to the one specific to our DotCom application.
 ///
 /// Use DeviceInfo.InitializeWithCustomImplementation() to set implementation instance to this.
 /// </summary>
 public class DeviceInfoHirDotcom : DeviceInfoWrapper
 {
 	public DeviceInfoHirDotcom(DeviceInfoBase wrappedInstance) : base(wrappedInstance) {}

 	public override ClientId ClientId => ClientId.WebGLStandaloneHir;

 }
 #endif	// UNITY_WEBGL