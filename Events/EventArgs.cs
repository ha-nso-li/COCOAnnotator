using COCOAnnotator.Records;
using System;
using System.Collections.Generic;

namespace COCOAnnotator.Events {
    public class CommitBboxEventArgs(IEnumerable<AnnotationRecord> Added, IEnumerable<AnnotationRecord> ChangedOldItems, IEnumerable<AnnotationRecord> ChangedNewItems, IEnumerable<AnnotationRecord> Deleted) : EventArgs {

        /// <summary>새로 추가 될 경계 상자입니다.</summary>
        public IEnumerable<AnnotationRecord> Added { get; } = Added;

        /// <summary>위치가 이동될 경계 상자입니다. 이동 전의 좌표 정보를 유지하고 있습니다.</summary>
        public IEnumerable<AnnotationRecord> ChangedOldItems { get; } = ChangedOldItems;

        /// <summary>
        /// 위치가 이동될 경계 상자의 새로운 위치입니다.
        /// <seealso cref="ChangedOldItems"/> 컬렉션 내에 같은 인덱스에 위치한 경계 상자의 이동 후 위치 정보를 담은 새로운 객체를 포함한 컬렉션입니다.
        /// </summary>
        public IEnumerable<AnnotationRecord> ChangedNewItems { get; } = ChangedNewItems;

        /// <summary>삭제될 경계 상자입니다.</summary>
        public IEnumerable<AnnotationRecord> Deleted { get; } = Deleted;
    }

    public class FailToLoadImageEventArgs(Uri ImageUri) : EventArgs {
        public Uri ImageUri { get; } = ImageUri;
    }
}
