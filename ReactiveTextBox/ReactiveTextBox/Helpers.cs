using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveTextBox
{
    public static class Helpers
    {
        public static async Task NullSafeAwait(Task task)
        {
            try
            {
                await task;
            }
            catch (NullReferenceException)
            {
                // NOTE: catching a NullReferenceException seems to be horrible hack;
                // however the other solution would be to lock for the given task when getting/setting its value, which just would make the code even more complex
            }
        }
    }
}
