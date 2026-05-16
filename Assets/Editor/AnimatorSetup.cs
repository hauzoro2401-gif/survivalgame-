using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Bước 4 - AnimatorSetup: Tạo Animator Controllers cơ bản.
/// Menu: Tools > CAVE RIFT > 2. Setup Animators
/// </summary>
public class AnimatorSetup : EditorWindow
{
    [MenuItem("Tools/CAVE RIFT/2. Setup Animators")]
    public static void SetupAnimators()
    {
        string[] chars = { "Minh", "Linh", "Khoa", "Trang" };
        string[] enemies = { "Gremvak", "BongDem" };

        if (!Directory.Exists("Assets/Animations/Characters"))
            Directory.CreateDirectory("Assets/Animations/Characters");
        if (!Directory.Exists("Assets/Animations/Enemies"))
            Directory.CreateDirectory("Assets/Animations/Enemies");

        int count = 0;

        // Characters
        foreach (string c in chars)
        {
            string path = $"Assets/Animations/Characters/{c}Animator.controller";
            CreateCharacterAnimator(path, c);
            count++;
        }

        // Enemies
        foreach (string e in enemies)
        {
            string path = $"Assets/Animations/Enemies/{e}Animator.controller";
            CreateEnemyAnimator(path, e);
            count++;
        }

        AssetDatabase.Refresh();
        Debug.Log($"✅ Đã tạo {count} Animator Controllers!");
    }

    private static void CreateCharacterAnimator(string path, string charName)
    {
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        // Parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        // States
        AnimatorState idleState = rootStateMachine.AddState("Idle");
        AnimatorState runState = rootStateMachine.AddState("Run");
        AnimatorState attackState = rootStateMachine.AddState("Attack");
        AnimatorState hurtState = rootStateMachine.AddState("Hurt");
        AnimatorState dieState = rootStateMachine.AddState("Die");

        rootStateMachine.defaultState = idleState;

        // Transitions: Idle <-> Run
        AnimatorStateTransition idleToRun = idleState.AddTransition(runState);
        idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToRun.hasExitTime = false;

        AnimatorStateTransition runToIdle = runState.AddTransition(idleState);
        runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        runToIdle.hasExitTime = false;

        // Transitions: Attack
        AnimatorStateTransition idleToAttack = idleState.AddTransition(attackState);
        idleToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        idleToAttack.hasExitTime = false;

        AnimatorStateTransition runToAttack = runState.AddTransition(attackState);
        runToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        runToAttack.hasExitTime = false;

        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;
        attackToIdle.exitTime = 1f;

        // Any State Transitions
        AnimatorStateTransition anyToHurt = rootStateMachine.AddAnyStateTransition(hurtState);
        anyToHurt.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
        anyToHurt.hasExitTime = false;

        AnimatorStateTransition hurtToIdle = hurtState.AddTransition(idleState);
        hurtToIdle.hasExitTime = true;
        hurtToIdle.exitTime = 1f;

        AnimatorStateTransition anyToDie = rootStateMachine.AddAnyStateTransition(dieState);
        anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
        anyToDie.hasExitTime = false;
        
        // Tự động gán AnimationClip nếu có
        AssignClips(controller, $"Assets/Sprites/Characters/{charName}");
    }

    private static void CreateEnemyAnimator(string path, string enemyName)
    {
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

        AnimatorState idleState = rootStateMachine.AddState("Idle");
        AnimatorState walkState = rootStateMachine.AddState("Walk");
        AnimatorState attackState = rootStateMachine.AddState("Attack");
        AnimatorState hurtState = rootStateMachine.AddState("Hurt");
        AnimatorState dieState = rootStateMachine.AddState("Die");

        rootStateMachine.defaultState = idleState;

        // Transitions
        AnimatorStateTransition idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToWalk.hasExitTime = false;

        AnimatorStateTransition walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        walkToIdle.hasExitTime = false;

        AnimatorStateTransition anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        anyToAttack.hasExitTime = false;
        AnimatorStateTransition attackToIdle = attackState.AddTransition(idleState);
        attackToIdle.hasExitTime = true;

        AnimatorStateTransition anyToHurt = rootStateMachine.AddAnyStateTransition(hurtState);
        anyToHurt.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
        anyToHurt.hasExitTime = false;
        AnimatorStateTransition hurtToIdle = hurtState.AddTransition(idleState);
        hurtToIdle.hasExitTime = true;

        AnimatorStateTransition anyToDie = rootStateMachine.AddAnyStateTransition(dieState);
        anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
        anyToDie.hasExitTime = false;
        
        // Assign Clips
        AssignClips(controller, $"Assets/Sprites/Enemies/{enemyName}");
    }

    private static void AssignClips(AnimatorController controller, string spriteFolderPath)
    {
        // Hàm này có thể được mở rộng để tự động tạo AnimationClip từ sprites
        // Hiện tại chỉ thiết lập cấu trúc State Machine cơ bản
    }
}
