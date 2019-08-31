// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

namespace Vlingo.Http.Tests.Sample.User.Model
{
    public class ProfileRepository
    {
        private static ProfileRepository _instance;
        
        private static volatile object _lockSync = new object();
        
        public static ProfileRepository Instance()
        {
            lock (_lockSync)
            {
                if (_instance == null)
                {
                    _instance = new ProfileRepository();
                }

                return _instance;
            }
        }
    }
}