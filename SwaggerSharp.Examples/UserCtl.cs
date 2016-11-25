using System;

namespace SwaggerSharp.Examples
{
    public class UserCtl: ControllerBase
    {
        [ActionVerb(Verb = "delete")]
        public int RemoveUser(string id)
        {
            return 1;
        }

        [ActionVerb(Verb = "post")]
        public int AddUser()
        {
            return 1;
        }

    }
}