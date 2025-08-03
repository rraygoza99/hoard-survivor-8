using Godot;
using System;

public partial class CameraFollow : Camera3D
{
	[Export]
	public NodePath TargetNodePath { get; set;}
	private Node3D _target;
	private Vector3 _offset;
	
	public override void _Ready(){
		if(TargetNodePath != null){
			_target = GetNode<Node3D>(TargetNodePath);
			
			_offset = GlobalTransform.Origin - _target.GlobalTransform.Origin;
		}
	}
	
	public override void _Process(double delta){
		if(_target != null){
			GlobalTransform = new Transform3D(GlobalTransform.Basis, _target.GlobalTransform.Origin + _offset);
		}
	}
}
