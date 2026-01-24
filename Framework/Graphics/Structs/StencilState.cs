namespace Foster.Framework;

/// <summary>
/// Specifies Stencil Operation for a Draw Command
/// </summary>
/// <param name="FailOp">Action to perform on samples that fail the stencil test</param>
/// <param name="PassOp">Action to perform on samples that pass the depth and stencil test</param>
/// <param name="DepthFailOp">Action to perform on samples that pass the stencil test and fail the depth test</param>
/// <param name="CompareOp">The comparison operator used in the stencil test</param>
public readonly record struct StencilState
(
	StencilOp FailOp,
	StencilOp PassOp,
	StencilOp DepthFailOp,
	DepthCompare CompareOp
)
{
	public StencilState(StencilOp Op, DepthCompare Compare)
		: this(Op, Op, Op, Compare) {}
}