﻿//#define PROFILE

using System;
using System.Collections;
using System.Threading;
using Svelto.ECS.Schedulers;
#region comment
///
/// Promoting a strict hierarchical namespace structure is an important part of the Svelto.ECS
/// Philosopy. EntityViews must be used by Engines belonging to the same namespace or parent
/// one
///  
#endregion
using Svelto.ECS.Vanilla.Example.SimpleEntityAsClass.SimpleEntity;
using Svelto.ECS.Vanilla.Example.SimpleEntityAsClass.SimpleEntityEngine;
using Svelto.ECS.Vanilla.Example.SimpleEntityAsStruct.SimpleEntityStruct;
using Svelto.ECS.Vanilla.Example.SimpleEntityAsStruct.SimpleEntityStructEngine;
using Svelto.WeakEvents;

#if PROFILE
using System.Diagnostics;
#endif

namespace Svelto.ECS.Vanilla.Example
{
    #region comment
    //the whole svelto framework is driven by the creation of Composition Root (see my articles for the definition)
    //One or more composition roots can be created inside a Context.
    //Since we are not using Unity for this example, the simplest context we can use is the Main entry point of the 
    //program
    #endregion
    public class Program
    {
        static void Main(string[] args)
        {
            simpleContext = new SimpleContext();
            
            while (true) Thread.Sleep(1000);
        }

        static SimpleContext simpleContext;
    }
    
    #region comment
    /// <summary>
    ///The Context is the framework starting point.
    ///As Composition root, it gives back to the coder the responsibility to create, 
    ///initialize and inject dependencies.
    ///Every application can have more than one context and every context can have one
    ///or more composition roots (a facade, but even a factory, can be a composition root)
    /// </summary>
    #endregion
    public class SimpleContext
    {
        #region comment
        /// <summary>
        /// Naivily we run the mainloop inside the constructor using Svelto.Tasks
        /// extension. Run() is the simple extension to run whatever IEnumerator.
        /// When used outside Unity, Run() starts on its own thread.
        /// </summary>
        #endregion
        public SimpleContext()
        {
            Run().Run();
        }

        IEnumerator Run()
        {
            #region comment  
            //An EngineRoot holds all the engines created so far and is 
            //responsible of the injection of the entity entityViews inside every
            //relative engine.
            //Every Composition Root can have one or more EnginesRoots. Spliting
            //EnginesRoots promote even more encapsulation than the ECS paradigm
            //itself already does
            #endregion

            var simpleSubmissionEntityViewScheduler = new SimpleSubmissionEntityViewScheduler();
            _enginesRoot = new EnginesRoot(simpleSubmissionEntityViewScheduler);

            #region comment
            //an EnginesRoot must never be injected inside other classes
            //that's why the IEntityFactory and IEntityFunctions implementation
            //are weakreference to the Engineroot.
            #endregion
            IEntityFactory entityFactory = _enginesRoot.GenerateEntityFactory();
            IEntityFunctions entityFunctions = _enginesRoot.GenerateEntityFunctions();

            //Add the Engine to manage the SimpleEntities
            _enginesRoot.AddEngine(new SimpleEngine(entityFunctions));
            //Add the Engine to manage the SimpleStructEntities
            _enginesRoot.AddEngine(new SimpleStructEngine());
            
            #region comment
            //the number of implementors to use to implement the /Entity components is arbitrary and it depends
            //by the modularity/reusability of the implementors.
            //You may avoid to create new implementors if you create modular/reusable ones
            //The concept of implementor is pretty unique in Svelto.ECS
            //and enable very interesting features. Refer to my articles to understand
            //more about it.
            #endregion
            object[] implementors = new object[1];
            int groupID = 0;

            ProfileIt<SimpleEntityDescriptor>((entityID) =>
                      {
                          //every entity must be implemented with its own implementor obviously
                          //(otherwise the instances will be shared between entities and
                          //we don't want that, right? :) )
                          implementors[0] = new SimpleImplementor("simpleEntity");

                          entityFactory.BuildEntity<SimpleEntityDescriptor>(entityID, implementors);
                      }, entityFactory);

            #region comment           
            //Entities as struct do not need an implementor. They are much more rigid
            //to use, but much faster. Please use them only when performance is really
            //critical (most of the time is not)
            #endregion
            ProfileIt<SimpleStructEntityDescriptor>((entityID) =>
                      {
                          entityFactory.BuildEntityInGroup<SimpleStructEntityDescriptor>(entityID, groupID);
                      }, entityFactory);
            
            implementors[0] = new SimpleImplementor(groupID);

            entityFactory.BuildEntityInGroup<SimpleGroupedEntityDescriptor>(0, groupID, implementors);
            
            #region comment
            //quick way to submit entities, this is not the standard way, but if you
            //create a custom EntitySubmissionScheduler is up to you to decide
            //when the EntityViews are submited to the engines and DB.
            #endregion
            simpleSubmissionEntityViewScheduler.SubmitEntities();

            Utility.Console.Log("Done - click any button to quit");

            Console.ReadKey();
            
            Environment.Exit(0);

            yield break;
        }
        
        #region comment
        /// <summary>
        ///with Unity there is no real reason to use any different than the 
        ///provided UnitySubmissionEntityViewScheduler. However Svelto.ECS
        ///has been written to be platform indipendent, so that you can
        ///write your own scheduler on another platform.
        ///The following scheduler has been made just for the sole purpose
        ///to show the simplest execution possible, which is add entityViews
        ///in the same moment they are built.
        /// </summary>
        #endregion
        class SimpleSubmissionEntityViewScheduler : EntitySubmissionScheduler
        {
            public void SubmitEntities()
            {
                _submitEntityViews.Invoke();
            }
            
            public override void Schedule(WeakAction submitEntityViews)
            {
                _submitEntityViews = submitEntityViews;
            }
            
            WeakAction _submitEntityViews;
        }

        void ProfileIt<T>(Action<int> action, IEntityFactory entityFactory) where T : IEntityDescriptor, new()
        {
#if PROFILE            
            var watch = new System.Diagnostics.Stopwatch();
    
            entityFactory.Preallocate<T>(10000000);

            watch.Start();
            
            for (var entityID = 0; entityID < 10000000; entityID++)
                action(entityID);
#else
                action(0);
#endif                
#if PROFILE            
            watch.Stop();

            Utility.Console.Log(watch.ElapsedMilliseconds.ToString());
#endif    
        }

        EnginesRoot _enginesRoot;
    }

    namespace SimpleEntityAsClass
    {
        namespace SimpleEntity
        {
            //just a custom component as proof of concept
            public interface ISimpleComponent
            {
                string name { get; }
                int groupID { get; set;  }
            }
            
            //the implementor(s) implement the components of the Entity. In Svelto.ECS
            //components are always interfaces when Entities as classes are used
            class SimpleImplementor : ISimpleComponent
            {
                public SimpleImplementor(int groupID)
                {
                    this.groupID = groupID;
                }
                
                public SimpleImplementor(string name)
                {
                    this.name = name;
                }

                public string name { get; }
                public int groupID
                {
                    get; set;
                }
            }
            #region comment
            //The EntityDescriptor identify your Entity. It's essential to identify
            //your entities with a name that comes from the Game Design domain.
            //More about this on my articles.
            #endregion
            class SimpleEntityDescriptor : GenericEntityDescriptor<SimpleEntityView>
            {}
            
            class SimpleGroupedEntityDescriptor : GenericEntityDescriptor<SimpleGroupedEntityView>
            {}
        }

        namespace SimpleEntityEngine
        {
            #region comment
            /// <summary>
            /// In order to show as many features as possible, I created this pretty useless Engine that
            /// accepts two Entities (so I can show the use of a MultiEntitiesViewEngine).
            /// Now, keep this in mind: an Engine should seldomly inherit from SingleEntityViewEngine
            /// or MultiEngineEntityViewsEngine.
            /// The Add and Remove callback per Entity added is another unique feature of Svelto.ECS
            /// and it's meant to be used if STRICTLY necessary. The feature has been mainly added
            /// to setup DispatchOnChange and DispatchOnSet (check my articles to know more), but
            /// it can be exploited for other reasons if well thought through!
            /// </summary>
            #endregion
            public class SimpleEngine : MultiEntityViewsEngine<SimpleEntityView, SimpleGroupedEntityView>
            {
                readonly IEntityFunctions _entityFunctions;

                public SimpleEngine(IEntityFunctions entityFunctions)
                {
                    _entityFunctions = entityFunctions;
                }

                /// <summary>
                /// With the following code I demostrate two features:
                /// First how to move an entity
                /// </summary>
                /// <param name="entityView"></param>
                protected override void Add(SimpleEntityView entityView)
                {
#if !PROFILE                    
                    Utility.Console.Log("EntityView Added");
    
                    _entityFunctions.RemoveEntity<SimpleEntityDescriptor>(entityView.ID);
#endif    
                }

                protected override void Remove(SimpleEntityView entityView)
                {
                    Utility.Console.Log(entityView.simpleComponent.name + "EntityView Removed");
                }

                protected override void Add(SimpleGroupedEntityView entityView)
                {
                    Utility.Console.Log("Grouped EntityView Added");
                    
                    _entityFunctions.SwapEntityGroup<SimpleGroupedEntityDescriptor>(entityView.ID, entityView.simpleComponent.groupID, 1);
                    entityView.simpleComponent.groupID = 1;
                    Utility.Console.Log("Grouped EntityView Swapped");
                    _entityFunctions.RemoveEntityFromGroup<SimpleGroupedEntityDescriptor>(entityView.ID, entityView.simpleComponent.groupID);
                }

                protected override void Remove(SimpleGroupedEntityView entityView)
                {
                    Utility.Console.Log("Grouped EntityView Removed");
                }
            }
            #region comment
            /// <summary>
            /// You must always design an Engine according the Entities it must handles.
            /// The Entities must be real application/game entities and cannot be
            /// abstract concepts (It would be very dangerous to create abstract/meaningless
            /// entities just for the purpose to write specific logic).
            /// An EntityView is just how an Engine see a specific Entity, this allows
            /// filtering of the components and promote abstraction/encapsulation
            /// </summary>
            #endregion
            public class SimpleEntityView : EntityView
            {
                public ISimpleComponent simpleComponent;
            }
            
            public class SimpleGroupedEntityView : EntityView
            {
                public ISimpleComponent simpleComponent;
            }
        }
    }

    namespace SimpleEntityAsStruct
    {
        //Let's not get things more complicated than they really are, in fact it's pretty
        //simple, an Entity can be made of Struct Components and/or Interface components
        //When you want build an entity entirely or partially upon struct components
        //you inherit from a MixedEntityDescriptor. In this case, you must explicitly
        //define which EntityView builder you want to use.
        //An EntityViewStructBuilder will create a SimpleEntityViewStruct
        namespace SimpleEntityStruct
        {
            class SimpleStructEntityDescriptor : MixedEntityDescriptor
                <EntityViewStructBuilder<SimpleEntityStructEngine.SimpleEntityViewStruct>>
            {}
        }

        namespace SimpleEntityStructEngine
        { 
            //An EntityViewStruct must always implement the IEntityStruct interface
            //don't worry, boxing/unboxing will never happen.
            struct SimpleEntityViewStruct : IEntityStruct
            {
                public int ID { get; set; }

                public int counter;
            }
            
            /// <summary>
            /// SingleEntityViewEngine and MultiEntityViewsEngine cannot be used with
            /// EntityView as struct as it would not make any sense. EntityViews as
            /// struct are meant to be use for tight high performance loops where
            /// cache coherence is considered during the design process
            /// </summary>
            public class SimpleStructEngine : IQueryingEntityViewEngine
            {
                public IEntityViewsDB entityViewsDB { private get; set; }

                public void Ready()
                {
                    Update().Run();
                }

                IEnumerator Update()
                {
                    Utility.Console.Log("Task Waiting");

                    while (true)
                    {
                        var entityViews = entityViewsDB.QueryGroupedEntityViewsAsArray<SimpleEntityViewStruct>(0, out int count);

                        if (count > 0)
                        {
                            for (int i = 0; i < count; i++)
                                AddOne(ref entityViews[i].counter);

                            Utility.Console.Log("Task Done");

                            yield break;
                        }

                        yield return null;
                    }
                }

                static void AddOne(ref int counter)
                {
                    counter += 1;
                }
            }
        }
    }
}

