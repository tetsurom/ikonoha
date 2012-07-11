
// written by shinpei nakata

K.import("konoha.float");
K.import("konoha.math");
K.import("konoha");
K.import("konoha.bytes");
K.import("konoha.string");
//K.import("posix.file");

int width = 64;
int height = 64;

int nsubsamples = 2;
int naosamples = 4;

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


class RayIntersection {
	Vec rayPosition;
	Vec rayDirection;
	Vec hitPosition;
	Vec normal;
	double distance;
	boolean  hit;
}

class Sphere {
	Vec center;
	double radius;

	Sphere(double x0, double y0, double z0, double r0) {
		this.center = new Vec(x0, y0, z0);
		this.radius = r0;
	}

	Array[Sphere] newArray(int size) {
		return this;
	}
}

void interSectsSphere(Sphere s, RayIntersection isect) {
	Vec rs = new Vec(isect.rayPosition.x - s.center.x, isect.rayPosition.y - s.center.y, isect.rayPosition.z - s.center.z);
	double b = dot(rs, isect.rayDirection);
	double c = dot(rs, rs) - s.radius * s.radius;
	double d = b * b - c;

	if (d > 0.0) {
		double t = -b - Math.sqrt(d);
		if ((t > 0.0) && (t < isect.distance)) {
			isect.distance = t;
			isect.hit = true;

			isect.hitPosition.x = isect.rayPosition.x + isect.rayDirection.x * t;
			isect.hitPosition.y = isect.rayPosition.y + isect.rayDirection.y * t;
			isect.hitPosition.z = isect.rayPosition.z + isect.rayDirection.z * t;

			isect.normal.x = isect.hitPosition.x - s.center.x;
			isect.normal.y = isect.hitPosition.y - s.center.y;
			isect.normal.z = isect.hitPosition.z - s.center.z;

			normalize(isect.normal);
		}
	}
}

class Plane {
	Vec position;
	Vec normal;
	Plane () {
	}
}
