using System;
using System.Collections;
using UnityEngine;

// Token: 0x02000200 RID: 512
public class GravityControl : MonoBehaviour
{
	// Token: 0x06000F1D RID: 3869 RVA: 0x0004D7EE File Offset: 0x0004BBEE
	private void Start()
	{
		Physics2D.gravity = new Vector2(0f, -30f);
		this.creditsUp = false;
	}

	// Token: 0x06000F1E RID: 3870 RVA: 0x0004D80C File Offset: 0x0004BC0C
	private void FixedUpdate()
	{
		if (Physics2D.gravity.y != 0f)
		{
			return;
		}
	}

	// Token: 0x06000F1F RID: 3871 RVA: 0x0004D834 File Offset: 0x0004BC34
	private void OnTriggerStay2D(Collider2D coll)
	{
		if (!this.creditsUp)
		{
			foreach (Transform transform in this.gravityWells)
			{
				this.gvec = (Vector3)transform.position - (Vector3)coll.attachedRigidbody.position;
				coll.attachedRigidbody.AddForce(2500f / this.gvec.sqrMagnitude * this.gvec.normalized);
			}
		}
	}

	// Token: 0x06000F20 RID: 3872 RVA: 0x0004D8B8 File Offset: 0x0004BCB8
	private void OnTriggerEnter2D(Collider2D coll)
	{
		Physics2D.gravity = new Vector2(0f, 0f);
	}

	// Token: 0x06000F21 RID: 3873 RVA: 0x0004D8D0 File Offset: 0x0004BCD0
	private void OnTriggerExit2D(Collider2D coll)
	{
		if (coll.attachedRigidbody.position.y > base.GetComponent<BoxCollider2D>().bounds.max.y - 5f)
		{
			if (!this.creditsUp)
			{
				Physics2D.gravity = new Vector2(0f, 1.2f);
				UnityEngine.Object.Instantiate<GameObject>(this.creditsPrefab, this.creditsParent);
				this.starNest.SetActive(true);
				this.starNest.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Brightness", 0f);
				base.StartCoroutine("FadeUpStarNest");
				this.creditsUp = true;
				PlayerPrefs.DeleteKey("NumSaves");
				PlayerPrefs.DeleteKey("SaveGame0");
				PlayerPrefs.DeleteKey("SaveGame1");
				PlayerPrefs.Save();
			}
		}
		else
		{
			Physics2D.gravity = new Vector2(0f, -30f);
		}
	}

	// Token: 0x06000F22 RID: 3874 RVA: 0x0004D9F0 File Offset: 0x0004BDF0
	private IEnumerator FadeUpStarNest()
	{
		float step = 2.0000001E-05f;
		for (float f = 0f; f <= 0.01f; f += step)
		{
			this.starNest.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Brightness", f);
			yield return null;
		}
		yield break;
	}

	// Token: 0x04000954 RID: 2388
	public Transform[] gravityWells;

	// Token: 0x04000955 RID: 2389
	private Vector2 gvec;

	// Token: 0x04000956 RID: 2390
	public GameObject creditsPrefab;

	// Token: 0x04000957 RID: 2391
	public Transform creditsParent;

	// Token: 0x04000958 RID: 2392
	private bool creditsUp;
    
	// Token: 0x0400095A RID: 2394
	public GameObject starNest;
    
    

	// Token: 0x0400095D RID: 2397
	public Camera fgCam;
}
