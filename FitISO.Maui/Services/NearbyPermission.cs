#if ANDROID
namespace FitISO.Maui.Services
{

    public class NearbyPermission : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions
        {
            get
            {
                var list = new List<(string, bool)>();

                if (OperatingSystem.IsAndroidVersionAtLeast(31))
                {
                    list.Add((global::Android.Manifest.Permission.BluetoothAdvertise, true));
                    list.Add((global::Android.Manifest.Permission.BluetoothConnect, true));
                    list.Add((global::Android.Manifest.Permission.BluetoothScan, true));
                }
                else
                {
                    list.Add((global::Android.Manifest.Permission.AccessFineLocation, true));
                }

                if (OperatingSystem.IsAndroidVersionAtLeast(33))
                    list.Add((global::Android.Manifest.Permission.NearbyWifiDevices, true));

                return list.ToArray();
            }
        }
    }
}
#endif