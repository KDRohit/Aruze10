//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Generated geometry class. All widgets have one.
/// This class separates the geometry creation into several steps, making it possible to perform
/// actions selectively depending on what has changed. For example, the widget doesn't need to be
/// rebuilt unless something actually changes, so its geometry can be cached. Likewise, the widget's
/// transformed coordinates only change if the widget's transform moves relative to the panel,
/// so that can be cached as well. In the end, using this class means using more memory, but at
/// the same time it allows for significant performance gains, especially when using widgets that
/// spit out a lot of vertices, such as UILabels.
/// </summary>

public class UIGeometry
{
	/// <summary>
	/// Widget's vertices (before they get transformed).
	/// </summary>

	public BetterList<Vector3> verts = new BetterList<Vector3>();

	/// <summary>
	/// Widget's texture coordinates for the geometry's vertices.
	/// </summary>

	public BetterList<Vector2> uvs = new BetterList<Vector2>();

	/// <summary>
	/// Array of colors for the geometry's vertices.
	/// </summary>

	public BetterList<Color32> cols = new BetterList<Color32>();

	// Relative-to-panel vertices, normal, and tangent
	BetterList<Vector3> mRtpVerts = new BetterList<Vector3>();
	Vector3 mRtpNormal;
	Vector4 mRtpTan;

	/// <summary>
	/// Whether the geometry contains usable vertices.
	/// </summary>

	public bool hasVertices { get { return (verts.size > 0); } }

	/// <summary>
	/// Whether the geometry has usable transformed vertex data.
	/// </summary>

	public bool hasTransformed { get { return (mRtpVerts != null) && (mRtpVerts.size > 0) && (mRtpVerts.size == verts.size); } }

	/// <summary>
	/// Step 1: Prepare to fill the buffers -- make them clean and valid.
	/// </summary>

	public void Clear ()
	{
		verts.Clear();
		uvs.Clear();
		cols.Clear();
		mRtpVerts.Clear();
	}

	/// <summary>
	/// Step 2: After the buffers have been filled, apply the specified pivot offset to the generated geometry.
	/// </summary>
#if USE_UNSAFE   
    unsafe
#endif    
	public void ApplyOffset (Vector3 pivotOffset)
	{
        if(pivotOffset.sqrMagnitude > 0.0001f)
        {
            Vector3 [] verts_buffer = verts.buffer;
            int size = verts.size;
#if USE_UNSAFE 
            fixed( float *src_vertsPtr_ = &verts_buffer[0].x )   
            {
                float *src_vertsPtr = src_vertsPtr_;
                float offx = pivotOffset.x;
                float offy = pivotOffset.y;
                float offz = pivotOffset.z;
                for (int i = 0; i < size; ++i) 
                {
                    *src_vertsPtr++ += offx;
                    *src_vertsPtr++ += offy;
                    *src_vertsPtr++ += offz;
                }
            }   
#else
    		for (int i = 0; i < size; ++i) 
                verts_buffer[i] += pivotOffset;
#endif        
        }
	}


	/// <summary>
	/// Step 3: Transform the vertices by the provided matrix.
	/// </summary>
#if USE_UNSAFE    
	unsafe public void ApplyTransform(Matrix4x4 widgetToPanel)
	{
        mRtpVerts.Clear();
        
		if (verts.size > 0)
		{
            int imax = verts.size;
            int imax8 = imax&~0x7;
            
            mRtpVerts.AddCount(imax);
            Vector3[] mRtpVerts_buffer = mRtpVerts.buffer;
            Vector3[] verts_buffer = verts.buffer;
            
            float *matfloat = (float*)&widgetToPanel;

            float z0 = matfloat[8];//widgetToPanel[0,2];
            float z1 = matfloat[9];//widgetToPanel[1,2];   
            float x2 = matfloat[2];//widgetToPanel[2,0];
            float y2 = matfloat[6];//widgetToPanel[2,1];
            float w2 = matfloat[14];//widgetToPanel[2,3];
            
            int i=0;
            
            float magic = z0+z1+x2-y2+w2; magic *= magic;
            if(magic < 0.0001f)
            {
                    float x0 = matfloat[0];//widgetToPanel[0,0];
                    float y0 = matfloat[4];//widgetToPanel[0,1];
                    float w0 = matfloat[12];//widgetToPanel[0,3];

                    float x1 = matfloat[1];//widgetToPanel[1,0];
                    float y1 = matfloat[5];//widgetToPanel[1,1];
                    float w1 = matfloat[13];//widgetToPanel[1,3];
                
                    float z2 = matfloat[10];//widgetToPanel[2,2];
            
                    fixed( float *src_vertsPtr_ = &verts_buffer[0].x ){
                    float *src_vertsPtr = src_vertsPtr_;
                    fixed( float *dst_vertsPtr_ = &mRtpVerts_buffer[0].x ){
                    float *dst_vertsPtr = dst_vertsPtr_;

                    float x,y,z;
                    for ( ; i < imax8; i+=8) 
                    {
                        x = *src_vertsPtr++;
                        y = *src_vertsPtr++;
                        z = *src_vertsPtr++;

                        *dst_vertsPtr++ = x * x0 + y * y0 + w0;
                        *dst_vertsPtr++ = x * x1 + y * y1 + w1;
                        *dst_vertsPtr++ = z * z2;
                        
                        x = *src_vertsPtr++;
                        y = *src_vertsPtr++;
                        z = *src_vertsPtr++;

                        *dst_vertsPtr++ = x * x0 + y * y0 + w0;
                        *dst_vertsPtr++ = x * x1 + y * y1 + w1;
                        *dst_vertsPtr++ = z * z2;
                        
                        x = *src_vertsPtr++;
                        y = *src_vertsPtr++;
                        z = *src_vertsPtr++;

                        *dst_vertsPtr++ = x * x0 + y * y0 + w0;
                        *dst_vertsPtr++ = x * x1 + y * y1 + w1;
                        *dst_vertsPtr++ = z * z2;
                        
                        x = *src_vertsPtr++;
                        y = *src_vertsPtr++;
                        z = *src_vertsPtr++;

                        *dst_vertsPtr++ = x * x0 + y * y0 + w0;
                        *dst_vertsPtr++ = x * x1 + y * y1 + w1;
                        *dst_vertsPtr++ = z * z2;
                        
                        x = *src_vertsPtr++;
                        y = *src_vertsPtr++;
                        z = *src_vertsPtr++;

                        *dst_vertsPtr++ = x * x0 + y * y0 + w0;
                        *dst_vertsPtr++ = x * x1 + y * y1 + w1;
                        *dst_vertsPtr++ = z * z2;
                        
                        x = *src_vertsPtr++;
                        y = *src_vertsPtr++;
                        z = *src_vertsPtr++;

                        *dst_vertsPtr++ = x * x0 + y * y0 + w0;
                        *dst_vertsPtr++ = x * x1 + y * y1 + w1;
                        *dst_vertsPtr++ = z * z2;
                        
                        x = *src_vertsPtr++;
                        y = *src_vertsPtr++;
                        z = *src_vertsPtr++;

                        *dst_vertsPtr++ = x * x0 + y * y0 + w0;
                        *dst_vertsPtr++ = x * x1 + y * y1 + w1;
                        *dst_vertsPtr++ = z * z2;
                        
                        x = *src_vertsPtr++;
                        y = *src_vertsPtr++;
                        z = *src_vertsPtr++;

                        *dst_vertsPtr++ = x * x0 + y * y0 + w0;
                        *dst_vertsPtr++ = x * x1 + y * y1 + w1;
                        *dst_vertsPtr++ = z * z2;
                     }
                        
                     for ( ; i < imax; i++) 
                     {
                        x = *src_vertsPtr++;
                        y = *src_vertsPtr++;
                        z = *src_vertsPtr++;

                        *dst_vertsPtr++ = x * x0 + y * y0 + w0;
                        *dst_vertsPtr++ = x * x1 + y * y1 + w1;
                        *dst_vertsPtr++ = z * z2;
                     }
     
                    //fixed    
                    }}

           }
           else
           {
                for ( ; i < imax8;) 
                {
                    mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
                    i++;
                    mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
                    i++;
                    mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
                    i++;
                    mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
                    i++;
                    mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
                    i++;
                    mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
                    i++;
                    mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
                    i++;
                    mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
                    i++;
                }

                //0 to 7
                for ( ; i < imax; ++i) 
                {
                    mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
                }
           }
     
			// Calculate the widget's normal and tangent
			mRtpNormal = widgetToPanel.MultiplyVector(Vector3.back).normalized;
			Vector3 tangent = widgetToPanel.MultiplyVector(Vector3.right).normalized;
			mRtpTan = new Vector4(tangent.x, tangent.y, tangent.z, -1f);
		}
	}
    
#else

	public void ApplyTransform (Matrix4x4 widgetToPanel)
	{
        mRtpVerts.Clear();
        
		if (verts.size > 0)
		{
            int imax = verts.size;
            mRtpVerts.AddCount(imax);
            Vector3[] mRtpVerts_buffer = mRtpVerts.buffer;
            Vector3[] verts_buffer = verts.buffer;
            
            //enroll
            int imax8 = imax&~0x7;
            
            //enrolled version of the loop - enrolled 8 times
            int i =0;
            for ( ; i < imax8;) 
            {
                mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
				i++;
                mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
				i++;
                mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
				i++;
                mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
				i++;
                mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
				i++;
                mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
				i++;
                mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
				i++;
                mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
				i++;
                    
            }
            
            //0 to 7
			for ( ; i < imax; ++i) 
            {
                mRtpVerts_buffer[i] = widgetToPanel.MultiplyPoint3x4(verts_buffer[i]);
            }

			// Calculate the widget's normal and tangent
			mRtpNormal = widgetToPanel.MultiplyVector(Vector3.back).normalized;
			Vector3 tangent = widgetToPanel.MultiplyVector(Vector3.right).normalized;
			mRtpTan = new Vector4(tangent.x, tangent.y, tangent.z, -1f);
		}
	}

#endif    
	/// <summary>
	/// Step 3: Fill the specified buffer using the transformed values.
	/// </summary>

	public void WriteToBuffers (BetterList<Vector3> v, BetterList<Vector2> u, BetterList<Color32> c, BetterList<Vector3> n, BetterList<Vector4> t)
	{
		if (mRtpVerts != null && mRtpVerts.size > 0)
		{
            int size = v.size;
            int count = mRtpVerts.size;
            v.AddCount(count);
            u.AddCount(count);
            c.AddCount(count);
            
            Array.Copy(mRtpVerts.buffer,0,v.buffer,size,count);
            Array.Copy(uvs.buffer,0,u.buffer,size,count);
            Array.Copy(cols.buffer,0,c.buffer,size,count);
            
     		if (n != null)
			{
                n.AddCount(count);
                t.AddCount(count);
            
                Vector3[] n_buffer = n.buffer;
                Vector4[] t_buffer = t.buffer;
            
                count+=size;
                
                for (int i = size; i < count; ++i)
				{
                    n_buffer[i] = mRtpNormal;
                    t_buffer[i] = mRtpTan;
				}
			}
		}
	}
}

