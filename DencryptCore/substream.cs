using System;
using System.IO;


// This stream starts after Salt + IV and ends before the HMAC to make sure that only the Encrypted Data gets decrypted.
// Basically allows the program to pick what parts it wants to read better than the default stream. (Wraps around an existing filestream)
public class SubStream : Stream
{
    private Stream _baseStream; // Original stream (A whole txt file, for example)
    private long _start; // Where substream begins in the base stream
    private long _length; // How long the substream is (How many bytes its allowed to read)
    private long _position; // How far it has read in the substream.

    public SubStream(Stream baseStream, long start, long length)
    {
        if (!baseStream.CanSeek)
            throw new ArgumentException("Base stream mus support seeking");

        if (start < 0 || length < 0)
            throw new ArgumentException("Start length must be non-negative");

        if (baseStream.Length < start + length)
            throw new ArgumentException("Base stream is too short");

        _baseStream = baseStream;
        _start = start;
        _length = length;
        _position = 0;

        _baseStream.Seek(_start, SeekOrigin.Begin);
    }

    public override bool CanRead => _baseStream.CanRead;
    public override bool CanSeek => true;
    public override bool CanWrite => false; // Read ONLY
    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set
        {
            if (value < 0 || value > _length)
                throw new ArgumentOutOfRangeException(nameof(value));
            _position = value;
            _baseStream.Seek(_start + _position, SeekOrigin.Begin);
        }
    }
    public override void Flush()
    {
        // Nothing yet, since its read-only
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_position >= _length)
            return 0; // End of file

        if (count > _length - _position) // Count is how many bytes the program wants to read, _length is how many bytes the SubStream gives to the program
            count = (int)(_length - _position);

        int read = _baseStream.Read(buffer, offset, count);
        _position += read; // Position increases for every byte read
        return read; // Returns how many bytes were read
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        long newPos;
        switch (origin)
        {
            case SeekOrigin.Begin:
                newPos = offset;
                break;
            case SeekOrigin.Current:
                newPos = _position + offset;
                break;
            case SeekOrigin.End:
                newPos = _length + offset;
                break;
            default:
                throw new ArgumentException("Invalid SeekOrigin");
        }

        if (newPos < 0 || newPos > _length)
            throw new IOException("Attempted to seek outside the bounds of the SubStream");

        _position = newPos;
        _baseStream.Seek(_start + _position, SeekOrigin.Begin);
        return _position;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("Cannot set length of a SubStream");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException("SubStream is read-only");
    }
}