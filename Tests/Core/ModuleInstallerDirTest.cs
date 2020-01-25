﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CKAN;
using log4net.Core;
using NUnit.Framework;
using Tests.Data;

namespace Tests.Core
{
    /// <summary>
    /// Tests the AddParentDirectories method in CKAN.ModuleInstaller
    /// </summary>
    [TestFixture]
    public class ModuleInstallerDirTest
    {
        private KSPManager           _manager;
        private DisposableKSP        _instance;
        private CKAN.RegistryManager _registryManager;
        private CKAN.Registry        _registry;
        private CKAN.ModuleInstaller _installer;
        private CkanModule           _testModule;
        private string               _gameDataDir;
        private IUser                _nullUser;

        /// <summary>
        /// Prep environment by setting up a single mod in
        /// a disposable KSP instance.
        /// </summary>
        [OneTimeSetUp]
        public void SetUp()
        {
            _testModule = TestData.DogeCoinFlag_101_module();

            _nullUser  = new NullUser();

            _manager   = new KSPManager(_nullUser);
            _instance  = new DisposableKSP();
            _registryManager = CKAN.RegistryManager.Instance(_instance.KSP);
            _registry  = _registryManager.registry;
            _installer = CKAN.ModuleInstaller.GetInstance(_instance.KSP, _manager.Cache, _nullUser);

            _gameDataDir = _instance.KSP.GameData();
            _registry.AddAvailable(_testModule);
            var testModFile = TestData.DogeCoinFlagZip();
            _manager.Cache.Store(_testModule, testModFile);
            _installer.InstallList(
                new List<string>() { _testModule.identifier },
                new RelationshipResolverOptions(),
                _registryManager
            );
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _manager.Dispose();
        }

        /// <summary>
        /// Make sure that the GameData directory is not included.
        /// </summary>
        [Test]
        public void TestGameData()
        {
            var result = _installer
                .AddParentDirectories(new HashSet<string>() { _gameDataDir })
                .ToList();

            Assert.IsEmpty(result);
        }

        /// <summary>
        /// These are sanity tests that attempt to throw exceptions
        /// when invoking AddParentDirectories()
        /// </summary>
        [Test]
        public void TestNullPath()
        {
            Assert.DoesNotThrow(delegate ()
            {
                _installer.AddParentDirectories(null);
                _installer.AddParentDirectories(new HashSet<string>());
                _installer.AddParentDirectories(new HashSet<string>() { string.Empty });
                _installer.AddParentDirectories(new HashSet<string>() { Path.GetPathRoot(Environment.CurrentDirectory) });
            });
        }

        /// <summary>
        /// Make sure that the list of directories is
        /// always normalized and deduplicated.
        /// </summary>
        [Test]
        public void TestSlashVariants()
        {
            var rawInstallDir = Path.Combine(_gameDataDir, _testModule.identifier);
            var normalizedInstallDir = CKAN.KSPPathUtils.NormalizePath(rawInstallDir);
            var windowsInstallDir = normalizedInstallDir.Replace('/', '\\');

            Assert.DoesNotThrow(delegate ()
            {
                var result = _installer.AddParentDirectories(new HashSet<string>()
                {
                    rawInstallDir,
                    windowsInstallDir,
                    normalizedInstallDir
                }).ToList();

                // should only contain one path
                Assert.AreEqual(1, result.Count);
                Assert.Contains(normalizedInstallDir, result);
            });
        }

        /// <summary>
        /// Try to add the same path with multiple casings, ensure no PathErrorKraken
        /// </summary>
        [Test]
        public void TestCaseSensitivity()
        {
            var paths = new HashSet<string>();
            // add in all-uppercase and all-lowercase version
            paths.Add(Path.Combine(_gameDataDir.ToUpper(), _testModule.identifier));
            paths.Add(Path.Combine(_gameDataDir.ToLower(), _testModule.identifier));
            // here we are looking for no PathErrorKraken
            Assert.DoesNotThrow(delegate()
            {
                var size = _installer.AddParentDirectories(paths).Count;
                // each directory adds two directories to the result
                // two directories each set { GAMEDATA and gamedata } = 4 objects in result array
                if (size != 4)
                {
                    throw new InvalidOperationException("Directories have case-sensitive differences");
                }
            });
        }
    }
}
