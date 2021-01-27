using System;
using System.Runtime.InteropServices;
namespace Nekres.Mumble_Info_Module
{
    public static class PInvoke
    {
        #region PInvoke

        [DllImport("USER32.dll")]
        private static extern short GetKeyState(uint vk);

        #endregion

        private const uint KEY_PRESSED = 0x8000;
        private const uint VK_LCONTROL = 0xA2;
        private const uint VK_LSHIFT = 0xA0;

        private static bool IsPressed(uint key) {
            return Convert.ToBoolean(GetKeyState(key) & KEY_PRESSED);
        }

        public static bool IsLControlPressed() {
            return Convert.ToBoolean(GetKeyState(VK_LCONTROL) & KEY_PRESSED);
        }

        public static bool IsLShiftPressed() {
            return Convert.ToBoolean(GetKeyState(VK_LSHIFT) & KEY_PRESSED);
        }
    }
}
