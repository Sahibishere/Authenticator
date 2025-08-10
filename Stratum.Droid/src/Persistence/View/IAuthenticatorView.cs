// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Threading.Tasks;
using Stratum.Core;
using Stratum.Core.Entity;
using Stratum.Droid.Interface;

namespace Stratum.Droid.Persistence.View
{
    public interface IAuthenticatorView : IReorderableView<Authenticator>
    {
        public string Search { get; set; }
        public CategorySelector CategorySelector { get; set; }
        public SortMode SortMode { get; set; }
        public Task LoadFromPersistenceAsync();
        public bool AnyWithoutFilter();
        public int IndexOf(Authenticator auth);
        public IEnumerable<AuthenticatorCategory> GetCurrentBindings();
        public void CommitRanking();
    }
}