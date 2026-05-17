using System;
using UnityEngine;

// Token: 0x020001FF RID: 511
public class GiftSpawner : MonoBehaviour
{
	// Token: 0x06000F19 RID: 3865 RVA: 0x0004D6F8 File Offset: 0x0004BAF8
	private void Start()
	{
		this.spawned = false;
		this.timeInTrigger = 0f;
	}

	// Token: 0x06000F1A RID: 3866 RVA: 0x0004D70C File Offset: 0x0004BB0C
	private void Update()
	{
	}

	// Token: 0x06000F1B RID: 3867 RVA: 0x0004D710 File Offset: 0x0004BB10
	private void OnTriggerStay2D(Collider2D coll)
	{
		if (coll.name != "PotCollider")
		{
			return;
		}
		if (this.spawned)
		{
			return;
		}
		this.timeInTrigger += Time.fixedDeltaTime;
		if (this.timeInTrigger > 480f)
		{
			if (this.player.position.x < base.transform.position.x)
			{
				this.gift.transform.position = this.spawn1.position;
				this.gift.SetActive(true);
			}
			else
			{
				this.gift.transform.position = this.spawn2.position;
				this.gift.SetActive(true);
			}
			this.spawned = true;
		}
	}

	// Token: 0x0400094E RID: 2382
	private float timeInTrigger;

	// Token: 0x0400094F RID: 2383
	private bool spawned;

	// Token: 0x04000950 RID: 2384
	public GameObject gift;

	// Token: 0x04000951 RID: 2385
	public Transform player;

	// Token: 0x04000952 RID: 2386
	public Transform spawn1;

	// Token: 0x04000953 RID: 2387
	public Transform spawn2;
}
