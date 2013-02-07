﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RC.Common.Diagnostics;
using System.Diagnostics;

namespace RC.UI
{
    /// <summary>
    /// Static class for defining and storing resources and resource groups.
    /// </summary>
    public static class UIResourceManager
    {
        /// <summary>
        /// Registers a resource in the given recource group with the given name.
        /// </summary>
        /// <param name="group">
        /// The name of the resource group. If no resource group exists with this name, a new group will
        /// be created automatically.
        /// </param>
        /// <param name="name">The name of the resource. This name must be unique across all the resources.</param>
        /// <param name="loader">Reference to the object that will load the resource.</param>
        public static void RegisterResource(string group, string name, UIResourceLoader loader)
        {
            if (runningTask != null) { throw new InvalidOperationException("There is a background task running!"); }
            if (string.IsNullOrEmpty(group)) { throw new ArgumentNullException("group"); }
            if (string.IsNullOrEmpty(name)) { throw new ArgumentNullException("name"); }
            if (loader == null) { throw new ArgumentNullException("loader"); }

            if (resources.ContainsKey(name)) { throw new UIException(string.Format("Resource with name '{0}' already registered at the UIResourceManager!", name)); }
            if (registeredLoaders.Contains(loader)) { throw new UIException("The given UIResourceLoader is already registered at the UIResourceManager!"); }

            if (!resourceGroups.ContainsKey(group))
            {
                /// A new resource group has to be created.
                resourceGroups.Add(group, new HashSet<string>());
            }

            /// Register the resource loader.
            resourceGroups[group].Add(name);
            registeredLoaders.Add(loader);
            resources.Add(name, loader);
        }

        /// <summary>
        /// Gets the resource with the given name.
        /// </summary>
        /// <typeparam name="T">The type of the resource to get.</typeparam>
        /// <param name="name">The unique name of the resource to get.</param>
        /// <returns>Reference to the resource.</returns>
        /// <exception cref="UIException">
        /// If the resource with the given name doesn't exist.
        /// If the type of the resource is incompatible with T.
        /// If the resource with the given name hasn't been loaded yet.
        /// </exception>
        public static T GetResource<T>(string name) where T : class
        {
            if (string.IsNullOrEmpty(name)) { throw new ArgumentNullException("name"); }
            if (!resources.ContainsKey(name)) { throw new UIException(string.Format("Resource '{0}' not registered!", name)); }

            return resources[name].GetResource<T>();
        }

        /// <summary>
        /// Loads every unloaded resources in the given resource group.
        /// </summary>
        /// <param name="group">The name of the resource group to load.</param>
        public static void LoadResourceGroup(string group)
        {
            if (string.IsNullOrEmpty(group)) { throw new ArgumentNullException("group"); }
            if (!resourceGroups.ContainsKey(group)) { throw new UIException(string.Format("Resource group '{0}' doesn't exist!", group)); }
            if (runningTask != null) { throw new InvalidOperationException("There is a background task running!"); }

            foreach (string resName in resourceGroups[group])
            {
                resources[resName].Load();
            }
        }

        /// <summary>
        /// Loads every unloaded resources in the given resource group. The loading will be performed as a background task.
        /// </summary>
        /// <param name="group">The name of the resource group to load.</param>
        /// <returns>
        /// Calling this method will return immediately with an interface to the background task. This interface can be used
        /// to subscribe the events of the task. The resource group loader task doesn't send messages during it's execution.
        /// </returns>
        /// <remarks>
        /// This method throws an InvalidOperationException if there is another resource group loader or unloader task in progress.
        /// </remarks>
        public static IUIBackgroundTask LoadResourceGroupAsync(string group)
        {
            if (string.IsNullOrEmpty(group)) { throw new ArgumentNullException("group"); }
            if (!resourceGroups.ContainsKey(group)) { throw new UIException(string.Format("Resource group '{0}' doesn't exist!", group)); }
            if (runningTask != null) { throw new InvalidOperationException("There is a background task running!"); }

            IEnumerator<string> resourceEnumerator = resourceGroups[group].GetEnumerator();
            runningTask = UITaskManager.StartTimeSharingTask(LoadResourceGroupAsync_i, resourceEnumerator);
            runningTask.Finished += EndOfTask;
            runningTask.Failed += EndOfTask;

            return runningTask;
        }

        /// <summary>
        /// Unloads every loaded resources in the given resource group.
        /// </summary>
        /// <param name="group">The name of the resource group to unload.</param>
        public static void UnloadResourceGroup(string group)
        {
            if (string.IsNullOrEmpty(group)) { throw new ArgumentNullException("group"); }
            if (!resourceGroups.ContainsKey(group)) { throw new UIException(string.Format("Resource group '{0}' doesn't exist!", group)); }
            if (runningTask != null) { throw new InvalidOperationException("There is a background task running!"); }

            foreach (string resName in resourceGroups[group])
            {
                resources[resName].Unload();
            }
        }

        /// <summary>
        /// Unloads every loaded resources in the given resource group. The unloading will be performed as a background task.
        /// </summary>
        /// <param name="group">The name of the resource group to unload.</param>
        /// <returns>
        /// Calling this method will return immediately with an interface to the background task. This interface can be used
        /// to subscribe the events of the task. The resource group unloader task doesn't send messages during it's execution.
        /// </returns>
        /// <remarks>
        /// This method throws an InvalidOperationException if there is another resource group loader or unloader task in progress.
        /// </remarks>
        public static IUIBackgroundTask UnloadResourceGroupAsync(string group)
        {
            if (string.IsNullOrEmpty(group)) { throw new ArgumentNullException("group"); }
            if (!resourceGroups.ContainsKey(group)) { throw new UIException(string.Format("Resource group '{0}' doesn't exist!", group)); }
            if (runningTask != null) { throw new InvalidOperationException("There is a background task running!"); }

            IEnumerator<string> resourceEnumerator = resourceGroups[group].GetEnumerator();
            runningTask = UITaskManager.StartTimeSharingTask(UnloadResourceGroupAsync_i, resourceEnumerator);
            runningTask.Finished += EndOfTask;
            runningTask.Failed += EndOfTask;

            return runningTask;
        }

        /// <summary>
        /// Deletes the whole resource group given in the parameter.
        /// </summary>
        /// <param name="group">The name of the resource group to delete.</param>
        /// <remarks>All resources in the given group will be unloaded automatically.</remarks>
        public static void DeleteResourceGroup(string group)
        {
            if (string.IsNullOrEmpty(group)) { throw new ArgumentNullException("group"); }
            if (!resourceGroups.ContainsKey(group)) { throw new UIException(string.Format("Resource group '{0}' doesn't exist!", group)); }
            if (runningTask != null) { throw new InvalidOperationException("There is a background task running!"); }

            foreach (string resName in resourceGroups[group])
            {
                resources[resName].Unload();
                resources.Remove(resName);
            }
            resourceGroups.Remove(group);
        }

        #region Internal methods

        /// <summary>
        /// Loads every unloaded resources in the given resource group.
        /// </summary>
        /// <param name="parameter">The enumerator of the resource group to load.</param>
        private static bool LoadResourceGroupAsync_i(object parameter)
        {
            IEnumerator<string> resourceEnumerator = (IEnumerator<string>)parameter;
            Stopwatch timer = new Stopwatch();
            timer.Start();
            while (resourceEnumerator.MoveNext())
            {
                resources[resourceEnumerator.Current].Load();
                if (timer.ElapsedMilliseconds > LOADING_RESOURCE_GROUP_CYCLE_TIME)
                {
                    /// Continue later
                    return true;
                }
            }

            /// Finished
            return false;
        }

        /// <summary>
        /// Unloads every loaded resources in the given resource group.
        /// </summary>
        /// <param name="parameter">The enumerator of the resource group to unload.</param>
        private static bool UnloadResourceGroupAsync_i(object parameter)
        {
            IEnumerator<string> resourceEnumerator = (IEnumerator<string>)parameter;
            Stopwatch timer = new Stopwatch();
            timer.Start();
            while (resourceEnumerator.MoveNext())
            {
                resources[resourceEnumerator.Current].Unload();
                if (timer.ElapsedMilliseconds > LOADING_RESOURCE_GROUP_CYCLE_TIME)
                {
                    /// Continue later
                    return true;
                }
            }

            /// Finished
            return false;
        }

        /// <summary>
        /// Called when the running task has completed (or failed).
        /// </summary>
        /// <param name="sender">The completed task.</param>
        /// <param name="message">The exception caught by the task in case of failure, otherwise null.</param>
        private static void EndOfTask(IUIBackgroundTask sender, object message)
        {
            runningTask = null;
        }

        #endregion Internal methods

        /// <summary>
        /// List of the registered resource loaders mapped by the names of the corresponding resources.
        /// </summary>
        private static Dictionary<string, UIResourceLoader> resources =
            new Dictionary<string, UIResourceLoader>();

        /// <summary>
        /// List of the resource groups.
        /// </summary>
        private static Dictionary<string, HashSet<string>> resourceGroups =
            new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// List of the every registered resource loaders.
        /// </summary>
        private static HashSet<UIResourceLoader> registeredLoaders = new HashSet<UIResourceLoader>();

        /// <summary>
        /// Reference to the currently running background task.
        /// </summary>
        private static IUIBackgroundTask runningTask = null;

        /// <summary>
        /// The duration of the timeslices of loading/unloading background tasks.
        /// </summary>
        private const int LOADING_RESOURCE_GROUP_CYCLE_TIME = 200;
    }
}