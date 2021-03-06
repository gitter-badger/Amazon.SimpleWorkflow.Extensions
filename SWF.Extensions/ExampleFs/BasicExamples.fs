﻿module BasicExamples

open System
open System.Collections.Generic

open Amazon.SimpleWorkflow.Extensions
open Amazon.SimpleWorkflow.Extensions.Model

let domain = "theburningmonk.com"

// #region Hello World example

let sayHelloWorld _ = printfn "Hello World!"; "Hello World!"

/// workflow which has only one activity - to print hello world when received an activity task
let helloWorldWorkflow =
    Workflow(domain = domain, name = "hello_world", 
             description = "simple 'hello world' example", 
             version = "1",
             identity = "Phantom") // you can optionally set the identity of your decision and activity workers
    ++> Activity("say_hello_world", "say 'hello world'", sayHelloWorld,
                 taskHeartbeatTimeout       = 60, 
                 taskScheduleToStartTimeout = 10,
                 taskStartToCloseTimeout    = 10, 
                 taskScheduleToCloseTimeout = 20)

// #endregion

// #region Echo example

let echo str = printfn "%s" str; str

let echoWorkflow =
    Workflow(domain = domain, name = "echo", 
             description = "simple echo example", 
             version = "1",
             execStartToCloseTimeout = 60, 
             taskStartToCloseTimeout = 30,
             childPolicy = ChildPolicy.Terminate)
    ++> Activity("echo", "echo input", echo,
                 taskHeartbeatTimeout       = 60, 
                 taskScheduleToStartTimeout = 10,
                 taskStartToCloseTimeout    = 10, 
                 taskScheduleToCloseTimeout = 20)

// #endregion

// #region Generic Activity example

let sum (arr : int[]) = arr |> Array.sum |> double

let genericActivityWorkflow =
    Workflow(domain = domain, name = "generic_activity", 
             description = "simple generic activity example", 
             version = "1")
    ++> Activity<int[], double>("sum", "sum int array into a double", sum,
                                taskHeartbeatTimeout       = 60, 
                                taskScheduleToStartTimeout = 10,
                                taskStartToCloseTimeout    = 10, 
                                taskScheduleToCloseTimeout = 20)

// #endregion

// #region Simple Pipeline example 
// i.e. a chain of activities where result from the previous activity is passed on
// as the input to the next activity

// the most boring conversation ever between me and another person.. ("@")/  \(*@*)
let greet me you = printfn "%s: hello %s!" me you; you
let bye me you = printfn "%s: good bye, %s!" me you; me

let simplePipelineWorkflow =
    Workflow(domain = domain, name = "simple_pipeline", 
             description = "simple pipeline example", 
             version = "1")
    ++> Activity("greet", "say hello", greet "Yan",
                 taskHeartbeatTimeout       = 60, 
                 taskScheduleToStartTimeout = 10,
                 taskStartToCloseTimeout    = 10, 
                 taskScheduleToCloseTimeout = 20)
    ++> Activity("bye", "say good bye", bye "Yan",
                 taskHeartbeatTimeout       = 60, 
                 taskScheduleToStartTimeout = 10,
                 taskStartToCloseTimeout    = 10, 
                 taskScheduleToCloseTimeout = 20)

// #endregion

// #region Child Workflow example
// i.e. kick off a child workflow from the main workflow, passing result from the
// last activity to the child workflow as input, and taking the result of the
// child workflow as input to the next activity

// sings the first part of the song and return the second part as result
let sing name = printfn "Old %s had a farm" name; "EE-I-EE-I-O"

// unlike the main workflow, child workflows MUST specify the timeouts and child
// policy on the workflow definition itself
let childWorkflow = 
    Workflow(domain = domain, name = "sing_along", 
             description = "child workflow to start a song", 
             version = "1",
             execStartToCloseTimeout = 60, 
             taskStartToCloseTimeout = 30,
             childPolicy = ChildPolicy.Terminate)
    ++> Activity("sing", "sing a song", sing,
                 taskHeartbeatTimeout       = 60, 
                 taskScheduleToStartTimeout = 10,
                 taskStartToCloseTimeout    = 10, 
                 taskScheduleToCloseTimeout = 20)

// this is the main workflow which
let withChildWorkflow =
    Workflow(domain = domain, name = "with_child_workflow", 
             description = "workflow which starts a child workflow in the middle", 
             version = "1")
    ++> Activity("greet", "say hello", greet "MacDonald",
                 taskHeartbeatTimeout       = 60, 
                 taskScheduleToStartTimeout = 10,
                 taskStartToCloseTimeout    = 10, 
                 taskScheduleToCloseTimeout = 20)
    ++> Activity("bye", "say good bye", bye "MacDonald",
                 taskHeartbeatTimeout       = 60, 
                 taskScheduleToStartTimeout = 10,
                 taskStartToCloseTimeout    = 10, 
                 taskScheduleToCloseTimeout = 20)
    ++> childWorkflow
    ++> Activity("echo", "echo the last part of the song", echo,
                 taskHeartbeatTimeout       = 60, 
                 taskScheduleToStartTimeout = 10,
                 taskStartToCloseTimeout    = 10, 
                 taskScheduleToCloseTimeout = 20)

// #endregion

// #region Failed Workflow (Activity) example
// you can specify a max number of attempts for each activity, the activity will be retried up
// to that many attempts (e.g. max 3 attempts = 1 attempt + 2 retries), if the activity failed
// or timed out on the last retry then it'll be failed and the whole workflow will be failed

let alwaysFail _ = failwith "oops"

let failedWorkflowWithActivity = 
    Workflow(domain = domain, name = "failed_workflow_with_activity", 
             description = "this workflow will fail because of its activity", 
             version = "1",
             execStartToCloseTimeout = 60, 
             taskStartToCloseTimeout = 30,
             childPolicy = ChildPolicy.Terminate)
    ++> Activity("always_fail", "this activity will always fail", alwaysFail,
                 taskHeartbeatTimeout       = 60, 
                 taskScheduleToStartTimeout = 10,
                 taskStartToCloseTimeout    = 10, 
                 taskScheduleToCloseTimeout = 20,
                 maxAttempts = 3)   // max 3 attempts, and fail the workflow after that

// #endregion

// #region Failed Workflow (ChildWorkflow) example
// the same retry mechanism can be applied to child workflows too

// unlike the main workflow, child workflows MUST specify the timeouts and child
// policy on the workflow definition itself
let alwaysFailChildWorkflow = 
    Workflow(domain = domain, name = "failed_child_workflow", 
             description = "this child workflow will always fail", 
             version = "1",
             execStartToCloseTimeout = 60, 
             taskStartToCloseTimeout = 30,
             childPolicy = ChildPolicy.Terminate,
             maxAttempts = 3)
    ++> Activity("always_fail", "this activity will always fail", alwaysFail,
                 taskHeartbeatTimeout       = 60, 
                 taskScheduleToStartTimeout = 10,
                 taskStartToCloseTimeout    = 10, 
                 taskScheduleToCloseTimeout = 20)

let failedWorkflowWithChildWorkflow = 
    Workflow(domain = domain, name = "failed_workflow_with_child_workflow", 
             description = "this workflow will fail because of its child workflow", 
             version = "1",
             execStartToCloseTimeout = 60, 
             taskStartToCloseTimeout = 30,
             childPolicy = ChildPolicy.Terminate)
    ++> alwaysFailChildWorkflow

// #endregion

// #region Parallel Activities example
// you can specify a number of activities to be run in parallel, the next stage of the workflow
// is triggered when all the parallel activities had completed successfully and their results
// aggregated into a single input using the supplied reducer

let echoActivity = Activity("echo", "echo", echo,
                            taskHeartbeatTimeout       = 60, 
                            taskScheduleToStartTimeout = 10,
                            taskStartToCloseTimeout    = 10, 
                            taskScheduleToCloseTimeout = 20)

let greetActivity = Activity("greet", "greet", greet "Yan",
                             taskHeartbeatTimeout       = 60, 
                             taskScheduleToStartTimeout = 10,
                             taskStartToCloseTimeout    = 10, 
                             taskScheduleToCloseTimeout = 20)

let byeActivity = Activity("bye", "bye", bye "Yan",
                           taskHeartbeatTimeout       = 60, 
                           taskScheduleToStartTimeout = 10,
                           taskStartToCloseTimeout    = 10, 
                           taskScheduleToCloseTimeout = 20)

let reducer (results : Dictionary<int, string>) =
    results |> Seq.map (fun kvp -> sprintf "Activity %d says [%s]" kvp.Key kvp.Value)
            |> (fun arr -> String.Join(";", arr))

let activities = 
    [| 
           echoActivity  :> ISchedulable
           greetActivity :> ISchedulable
           byeActivity   :> ISchedulable 
    |]

let parallelActivities = 
    Workflow(domain = domain, name = "parallel_activities", 
             description = "this workflow runs several activities in parallel", 
             version = "1")
    ++> (activities, reducer)
    ++> echoActivity

// #endregion

// #region Parallel Schedulables example
// you can schedule a mixture of activities and child workflows in parallels

let schedulables = 
    [| 
           echoActivity  :> ISchedulable
           echoWorkflow  :> ISchedulable
           childWorkflow :> ISchedulable
           greetActivity :> ISchedulable
    |]

let parallelSchedulables = 
    Workflow(domain = domain, name = "parallel_schedulables", 
             description = "this workflow runs several activities and workflows in parallel", 
             version = "1")
    ++> echoActivity
    ++> (schedulables, reducer)
    ++> echoActivity

// #endregion

// #region Parallel Schedulables with Error example
// if one of the workflow/activities failed then the whole workflow fails

let failedSchedulables = 
    [| 
           echoActivity  :> ISchedulable
           echoWorkflow  :> ISchedulable
           childWorkflow :> ISchedulable
           greetActivity :> ISchedulable
           failedWorkflowWithActivity :> ISchedulable
    |]

let failedParallelSchedulables = 
    Workflow(domain = domain, name = "failed_parallel_schedulables", 
             description = "this workflow runs several activities and workflows in parallel and one of them fails", 
             version = "1")
    ++> echoActivity
    ++> (failedSchedulables, reducer)
    ++> echoActivity

// #endregion
