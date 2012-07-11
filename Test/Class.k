K.import("konoha.float");
K.import("konoha.math");
K.import("konoha");
K.import("konoha.bytes");
K.import("konoha.string");

class Vec {
	float x;
	float y;
	float z;
	Vec (float x0, float y0, float z0) {
		this.x = x0;
		this.y = y0;
		this.z = z0;
	}
}

float dot (Vec v0, Vec v1) {
	return v0.x * v1.x + v0.y * v1.y + v0.z * v1.z;
}

void cross(Vec v0, Vec v1, Vec v2) {
	v2.x = v0.y * v1.z - v0.z * v1.y;
	v2.y = v0.z * v1.x - v0.x * v1.z;
	v2.z = v0.x * v1.y - v0.y * v1.x;
}

void normalize(Vec v1) {
	double q = dot(v1, v1);
	double length = Math.sqrt(q);
	if (length > 1.0e-10) {
		v1.x = v1.x / length;
		v1.y = v1.y / length;
		v1.z = v1.z / length;
	}
}

