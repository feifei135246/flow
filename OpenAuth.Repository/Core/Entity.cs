using System;

namespace OpenAuth.Repository.Core
{
    public abstract class Entity
    {
        public string Id { get; set; }

        //public string UninId { get; set; }

        public Entity()
        {
            Id = Guid.NewGuid().ToString();
            //UninId = "";
        }
    }
}
